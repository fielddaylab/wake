using System;
using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Modeling {
    public class ModelWorldDisplay : MonoBehaviour, IScenePreloader {

        #region Types

        [Serializable] private class OrganismPool : SerializablePool<ModelOrganismDisplay> { }
        [Serializable] private class ConnectionPool : SerializablePool<ModelConnectionDisplay> { }

        private unsafe struct GraphSolverState {
            public Vector2* Positions;
            public Vector2* Forces;
            public uint* ConnectionMasks;
            public uint MovedOutputMask;
        }

        #endregion // Types

        #region Consts

        private const int MaxGraphNodes = 16;
        private const int MaxSolverIterations = 64;
        private const float SolverVelocityThresholdSq = .2f * .2f;

        #endregion // Consts

        #region Inspector

        [Header("Root")]
        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private InputRaycasterLayer m_Input = null;

        [Header("Water Properties")]
        [SerializeField] private ModelWaterPropertyDisplay m_LightProperty = null;
        [SerializeField] private ModelWaterPropertyDisplay m_TemperatureProperty = null;
        [SerializeField] private ModelWaterPropertyDisplay m_PHProperty = null;
        [SerializeField] private ModelWaterPropertyDisplay m_OxygenProperty = null;
        [SerializeField] private ModelWaterPropertyDisplay m_CarbonDioxideProperty = null;

        [Header("Organisms")]
        [SerializeField] private OrganismPool m_OrganismPool = null;
        [SerializeField] private ConnectionPool m_ConnectionPool = null;

        [Header("Constants")]
        [SerializeField] private float m_PositionScale = 96;
        [SerializeField] private float m_ForceMultiplier = 1;
        [SerializeField] private float m_RepulsiveForce = 1024;
        [SerializeField] private float m_SpringForce = 1024;
        [SerializeField] private float m_IdealSpringLength = 256;
        [SerializeField] private float m_GravityForce = 10;

        #endregion // Inspector

        private ModelState m_State;
        private Routine m_ShowHideRoutine;
        private AsyncHandle m_ReconstructHandle;
        private TransformState m_OriginalCanvasState;
        [NonSerialized] private StringHash32 m_LastConstructedId;

        private readonly Dictionary<StringHash32, ModelOrganismDisplay> m_OrganismMap = new Dictionary<StringHash32, ModelOrganismDisplay>();
        private readonly ModelWaterPropertyDisplay[] m_WaterChemMap = new ModelWaterPropertyDisplay[(int) WaterPropertyId.TRACKED_COUNT];
        private GraphSolverState m_SolverState;

        private unsafe ModelWorldDisplay() {
            m_SolverState.Positions = Unsafe.AllocArray<Vector2>(MaxGraphNodes * 2);
            m_SolverState.Forces = m_SolverState.Positions + MaxGraphNodes;
            m_SolverState.ConnectionMasks = Unsafe.AllocArray<uint>(MaxGraphNodes);
        }

        unsafe ~ModelWorldDisplay() {
            Unsafe.Free(m_SolverState.Positions);
            Unsafe.Free(m_SolverState.ConnectionMasks);
        }

        private void Awake() {
            m_WaterChemMap[(int) WaterPropertyId.Light] = m_LightProperty;
            m_WaterChemMap[(int) WaterPropertyId.Temperature] = m_TemperatureProperty;
            m_WaterChemMap[(int) WaterPropertyId.PH] = m_PHProperty;
            m_WaterChemMap[(int) WaterPropertyId.Oxygen] = m_OxygenProperty;
            m_WaterChemMap[(int) WaterPropertyId.CarbonDioxide] = m_CarbonDioxideProperty;

            m_OriginalCanvasState = TransformState.LocalState(m_Canvas.transform);

            Services.Events.Register(GameEvents.WaterPropertiesUpdated, UpdateWaterPropertiesUnlocked, this);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        public void SetData(ModelState state) {
            m_State = state;
        }

        public void Show() {
            m_Canvas.enabled = true;
            m_ShowHideRoutine.Replace(this, m_Group.FadeTo(1, 0.2f));
            m_Input.Override = null;
            UpdateWaterPropertiesUnlocked();
            if (m_LastConstructedId != m_State.Environment.Id()) {
                SyncEnvironmentChemistry(m_State.Environment.GetEnvironment());
                Reconstruct();
            }
        }

        public void Hide() {
            m_ShowHideRoutine.Replace(this, m_Group.FadeTo(0, 0.2f)).OnComplete(OnHideCompleted);
            m_Input.Override = false;
        }

        private void OnHideCompleted() {
            m_Canvas.enabled = false;
            if (m_ReconstructHandle.IsRunning()) {
                m_ReconstructHandle.Cancel();
                m_OrganismPool.Reset();
                m_ConnectionPool.Reset();
                m_LastConstructedId = null;
            }
        }

        #if UNITY_EDITOR

        private void LateUpdate() {
            if (m_Canvas.enabled && m_Input.Device.KeyPressed(KeyCode.Y)) {
                Reconstruct();
            }
        }

        #endif // UNITY_EDITOR

        #region Reconstruction
        
        public IEnumerator Reconstruct() {
            m_ReconstructHandle.Cancel();
            m_LastConstructedId = m_State.Environment.Id();
            m_ReconstructHandle = Async.Schedule(ReconstructProcess(), AsyncFlags.HighPriority | AsyncFlags.MainThreadOnly);
            return m_ReconstructHandle;
        }

        private IEnumerator ReconstructProcess() {
            m_OrganismPool.Reset();
            m_ConnectionPool.Reset();
            m_OrganismMap.Clear();
            yield return null;

            int organismCount = m_State.Conceptual.GraphedEntities.Count;
            if (organismCount == 0) {
                yield break;
            }

            Assert.True(organismCount <= MaxGraphNodes, "More than maximum allowed critters ({0}) in graph - attempting to populate with {1} nodes", organismCount, MaxGraphNodes);
            
            ModelOrganismDisplay display;
            int index = 0;
            foreach(var organism in m_State.Conceptual.GraphedEntities) {
                display = AllocOrganism(organism, index++);
                yield return null;
            }
            m_SolverState.MovedOutputMask = (1u << organismCount) - 1;
            foreach(var fact in m_State.Conceptual.GraphedFacts) {
                switch(fact.Type) {
                    case BFTypeId.Eat: {
                        GenerateConnection(fact, fact.Parent, ((BFEat) fact).Critter);
                        break;
                    }
                }
                yield return null;
            }

            yield return 1;

            using(Profiling.Time("solving conceptual model graph")) {
                int iterations = MaxSolverIterations;
                while(m_SolverState.MovedOutputMask != 0 && iterations > 0) {
                    iterations--;
                    SolveStep(ref m_SolverState, organismCount);
                    yield return null;
                }
                if (m_SolverState.MovedOutputMask != 0) {
                    Log.Warn("[ModelWorldDisplay] Graph layout did not reach equilibrium in {0} iterations", MaxSolverIterations - iterations);
                } else {
                    Log.Msg("[ModelWorldDisplay] Solved graph layout in {0} iterations", MaxSolverIterations - iterations);
                }
            }

            UpdateOrganismPositions(organismCount);
            yield return null;
            UpdateConnectionPositions();
        }

        private unsafe ModelOrganismDisplay AllocOrganism(BestiaryDesc desc, int index) {
            ModelOrganismDisplay display = m_OrganismPool.Alloc();
            display.Initialize(desc, index);
            m_OrganismMap.Add(desc.Id(), display);
            m_SolverState.Positions[index] = new Vector2(RNG.Instance.NextFloat(-2, 2), RNG.Instance.NextFloat(-2, 2));
            m_SolverState.Forces[index] = default;
            m_SolverState.ConnectionMasks[index] = 0;
            return display;
        }

        private unsafe void GenerateConnection(BFBase fact, BestiaryDesc owner, BestiaryDesc target) {
            int indexA = m_OrganismMap[owner.Id()].Index;
            int indexB = m_OrganismMap[target.Id()].Index;
            m_SolverState.ConnectionMasks[indexA] |= (1u << indexB);
            m_SolverState.ConnectionMasks[indexB] |= (1u << indexA);

            ModelConnectionDisplay connection = m_ConnectionPool.Alloc();
            connection.Fact = fact;
            connection.IndexA = indexA;
            connection.IndexB = indexB;
        }

        private void UpdateOrganismPositions(int count) {
            var allocatedOrganisms = m_OrganismPool.ActiveObjects;
            for(int i = 0; i < count; i++) {
                PositionOrganism(allocatedOrganisms[i], ref m_SolverState, i);
            }
        }

        private void UpdateConnectionPositions() {
            var allocatedConnections = m_ConnectionPool.ActiveObjects;
            var allocatedOrganisms = m_OrganismPool.ActiveObjects;
            int count = allocatedConnections.Count;
            Vector2 a, b, vecAB, centerAB;
            float distAB;
            ModelConnectionDisplay display;
            for(int i = 0; i < count; i++) {
                display = allocatedConnections[i];
                a = allocatedOrganisms[display.IndexA].Transform.anchoredPosition;
                b = allocatedOrganisms[display.IndexB].Transform.anchoredPosition;
                vecAB = b - a;
                centerAB = (a + b) * 0.5f;
                distAB = vecAB.magnitude;
                distAB -= m_PositionScale * 1.1f;
                display.Transform.SetSizeDelta(distAB, Axis.X);
                display.Transform.SetAnchorPos(centerAB);
                display.Transform.SetRotation(Mathf.Atan2(vecAB.y, vecAB.x) * Mathf.Rad2Deg, Axis.Z, Space.Self);
                display.Transform.SetAsFirstSibling();
            }
        }

        private unsafe void PositionOrganism(ModelOrganismDisplay display, ref GraphSolverState solverState, int index) {
            display.Transform.SetAnchorPos(m_SolverState.Positions[index] * m_PositionScale, Axis.XY);
        }

        private unsafe void SolveStep(ref GraphSolverState solverState, int count) {
            solverState.MovedOutputMask = 0;
            Vector2 posA, posB, vecAB, vecForce;
            Vector2* forceA, forceB;
            int a, b;
            uint maskA;
            float distAB;
            float tempForce;

            // update velocities
            for(a = 0; a < count; a++) {
                posA = solverState.Positions[a];
                forceA = &solverState.Forces[a];
                maskA = solverState.ConnectionMasks[a];

                // gravity force
                posB = default(Vector2);
                vecAB = -posA;
                *forceA += vecAB * m_GravityForce;

                for(b = a + 1; b < count; b++) {
                    posB = solverState.Positions[b];
                    forceB = &solverState.Forces[b];

                    vecAB = posB - posA; // a to b
                    distAB = vecAB.magnitude;

                    // spring force
                    if ((maskA & (1 << b)) != 0) {
                        tempForce = m_SpringForce * Mathf.Log(distAB / m_IdealSpringLength);
                        vecForce = vecAB.normalized * tempForce;
                        *forceA += vecForce;
                        *forceB -= vecForce;
                    } else { // repulse force
                        tempForce = m_RepulsiveForce / (distAB * distAB);
                        vecForce = vecAB.normalized * tempForce;
                        *forceA -= vecForce;
                        *forceB += vecForce;
                    }
                }
            }

            // update positions
            for(a = 0; a < count; a++) {
                forceA = &solverState.Forces[a];
                solverState.Positions[a] += *forceA * m_ForceMultiplier;
                if (forceA->sqrMagnitude > SolverVelocityThresholdSq) {
                    solverState.MovedOutputMask |= (1u << a);
                }
                *forceA = default;
            }
        }

        #endregion // Reconstruction

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            m_LightProperty.Initialize(Assets.Property(WaterPropertyId.Light));
            m_TemperatureProperty.Initialize(Assets.Property(WaterPropertyId.Temperature));
            m_PHProperty.Initialize(Assets.Property(WaterPropertyId.PH));
            m_OxygenProperty.Initialize(Assets.Property(WaterPropertyId.Oxygen));
            m_CarbonDioxideProperty.Initialize(Assets.Property(WaterPropertyId.CarbonDioxide));
            yield return null;
            m_Canvas.enabled = false;
            m_Group.alpha = 0;
            m_Input.Override = false;
            yield return null;
            m_ConnectionPool.Initialize();
            m_OrganismPool.Initialize();
        }

        private void UpdateWaterPropertiesUnlocked() {
            var save = Save.Inventory;
            m_LightProperty.gameObject.SetActive(save.IsPropertyUnlocked(WaterPropertyId.Light));
            m_TemperatureProperty.gameObject.SetActive(save.IsPropertyUnlocked(WaterPropertyId.Temperature));
            m_PHProperty.gameObject.SetActive(save.IsPropertyUnlocked(WaterPropertyId.PH));
            m_OxygenProperty.gameObject.SetActive(save.IsPropertyUnlocked(WaterPropertyId.Oxygen));
            m_CarbonDioxideProperty.gameObject.SetActive(save.IsPropertyUnlocked(WaterPropertyId.CarbonDioxide));
        }

        private void SyncEnvironmentChemistry(WaterPropertyBlockF32 environment) {
            m_LightProperty.SetValue(environment.Light);
            m_TemperatureProperty.SetValue(environment.Temperature);
            m_PHProperty.SetValue(environment.PH);
            m_OxygenProperty.SetValue(environment.Oxygen);
            m_CarbonDioxideProperty.SetValue(environment.CarbonDioxide);
        }
    }
}