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
            public Unsafe.ArenaHandle Allocator;
            public Vector2* Positions;
            public Vector2* Forces;
            public uint* ConnectionMasks;
            public uint MovedOutputMask;
            public Vector2* FixedPropertyPositions;
        }

        #endregion // Types

        #region Consts

        private const int MaxOrganismNodes = 16;
        private const int MaxPropertyNodes = (int) WaterProperties.TrackedMax + 1;
        private const int MaxGraphNodes = MaxOrganismNodes + MaxPropertyNodes;
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
        [SerializeField] private float m_ConnectionOffsetFactor = 1.25f;
        // [SerializeField] private float m_BoundaryForce = 2;

        [Header("Textures")]
        [SerializeField] private Texture2D m_PartialLineTexture = null;
        [SerializeField] private Texture2D m_FullLineTexture = null;

        #endregion // Inspector

        private ModelState m_State;
        private Routine m_ShowHideRoutine;
        private AsyncHandle m_ReconstructHandle;
        private TransformState m_OriginalCanvasState;
        [NonSerialized] private StringHash32 m_LastConstructedId;
        [NonSerialized] private BestiaryDesc m_LastConstructedInterventionTarget;

        private readonly Dictionary<StringHash32, ModelOrganismDisplay> m_OrganismMap = new Dictionary<StringHash32, ModelOrganismDisplay>();
        private readonly ModelWaterPropertyDisplay[] m_WaterChemMap = new ModelWaterPropertyDisplay[(int) WaterPropertyId.TRACKED_COUNT];
        private GraphSolverState m_SolverState;

        private readonly ModelOrganismDisplay.OnAddRemoveDelegate m_OrganismInterventionDelegate;

        private unsafe ModelWorldDisplay() {
            m_SolverState.Allocator = Unsafe.CreateArena(1024);
            m_SolverState.Positions = Unsafe.AllocArray<Vector2>(m_SolverState.Allocator, MaxGraphNodes);
            m_SolverState.Forces = Unsafe.AllocArray<Vector2>(m_SolverState.Allocator, MaxGraphNodes);
            m_SolverState.ConnectionMasks = Unsafe.AllocArray<uint>(m_SolverState.Allocator, MaxGraphNodes);
            m_SolverState.FixedPropertyPositions = Unsafe.AllocArray<Vector2>(m_SolverState.Allocator, MaxPropertyNodes);
            m_OrganismInterventionDelegate = OnOrganismRequestAddRemove;
        }

        unsafe ~ModelWorldDisplay() {
            Unsafe.TryFreeArena(ref m_SolverState.Allocator);
        }

        private void Awake() {
            m_WaterChemMap[(int) WaterPropertyId.Light] = m_LightProperty;
            m_WaterChemMap[(int) WaterPropertyId.Temperature] = m_TemperatureProperty;
            m_WaterChemMap[(int) WaterPropertyId.PH] = m_PHProperty;
            m_WaterChemMap[(int) WaterPropertyId.Oxygen] = m_OxygenProperty;
            m_WaterChemMap[(int) WaterPropertyId.CarbonDioxide] = m_CarbonDioxideProperty;
            InitFixedPositions();

            m_OriginalCanvasState = TransformState.LocalState(m_Canvas.transform);
        }

        private unsafe void InitFixedPositions() {
            m_SolverState.FixedPropertyPositions[(int) WaterPropertyId.Light] = GetPositionInv(m_LightProperty);
            m_SolverState.FixedPropertyPositions[(int) WaterPropertyId.Temperature] = GetPositionInv(m_TemperatureProperty);
            m_SolverState.FixedPropertyPositions[(int) WaterPropertyId.PH] = GetPositionInv(m_PHProperty);
            m_SolverState.FixedPropertyPositions[(int) WaterPropertyId.Oxygen] = GetPositionInv(m_OxygenProperty);
            m_SolverState.FixedPropertyPositions[(int) WaterPropertyId.CarbonDioxide] = GetPositionInv(m_CarbonDioxideProperty);
        }

        public void SetData(ModelState state) {
            m_State = state;
            m_State.OnPhaseChanged += OnPhaseChanged;
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

        public void EnableIntervention() {
            UpdateInterventionControls();
        }

        public void DisableIntervention() {
            foreach(var display in m_OrganismPool.ActiveObjects) {
                display.DisableIntervention();
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

        public void ReconstructForIntervention() {
            bool prevExtraConstruct = m_LastConstructedInterventionTarget != null && !m_State.Conceptual.SimulatedEntities.Contains(m_LastConstructedInterventionTarget);
            bool newExtraConstruct = m_State.Simulation.IsInterventionNewOrganism();
            if (prevExtraConstruct && newExtraConstruct) {
                if (m_LastConstructedInterventionTarget == m_State.Simulation.Intervention.Target) {
                    return;
                }
            } else if (!prevExtraConstruct && !newExtraConstruct) {
                return;
            }
            
            Reconstruct();
        }
        
        public IEnumerator Reconstruct() {
            m_ReconstructHandle.Cancel();
            m_LastConstructedId = m_State.Environment.Id();
            m_ReconstructHandle = Async.Schedule(ReconstructProcess(), AsyncFlags.HighPriority | AsyncFlags.MainThreadOnly);
            m_LastConstructedInterventionTarget = m_State.Simulation.Intervention.Target;
            return m_ReconstructHandle;
        }

        private IEnumerator ReconstructProcess() {
            m_OrganismPool.Reset();
            m_ConnectionPool.Reset();
            m_OrganismMap.Clear();
            m_Input.Override = false;
            yield return null;

            int organismCount = m_State.Conceptual.GraphedEntities.Count;
            if (organismCount == 0) {
                yield break;
            }

            Assert.True(organismCount <= MaxOrganismNodes, "More than maximum allowed critters ({0}) in graph - attempting to populate with {1} nodes", organismCount, MaxOrganismNodes);
            
            ModelOrganismDisplay display;
            int index = 0;
            foreach(var organism in m_State.Conceptual.GraphedEntities) {
                display = AllocOrganism(organism, index++);
                yield return null;
            }

            var intervention = m_State.Simulation.Intervention;
            foreach(var entity in intervention.AdditionalEntities) {
                if (!m_State.Conceptual.GraphedEntities.Contains(entity)) {
                    organismCount++;
                    display = AllocOrganism(entity, index++);
                    yield return null;
                }
            }

            m_SolverState.MovedOutputMask = (1u << organismCount) - 1;
            foreach(var fact in m_State.Conceptual.GraphedFacts) {
                if (!BFType.IsBehavior(fact)) {
                    continue;
                }

                BestiaryDesc target = BFType.Target(fact);
                if (target != null) {
                    GenerateConnection(fact, fact.Parent, target, Save.Bestiary.GetDiscoveredFlags(fact.Id));
                }
                yield return null;
            }

            foreach(var fact in intervention.AdditionalFacts) {
                if (m_State.Conceptual.GraphedFacts.Contains(fact) || !BFType.IsBehavior(fact)) {
                    continue;
                }

                BestiaryDesc target = BFType.Target(fact);
                if (target != null) {
                    GenerateConnection(fact, fact.Parent, target, Save.Bestiary.GetDiscoveredFlags(fact.Id));
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
            yield return null;
            UpdateInterventionControls();
            yield return null;

            m_Input.Override = null;
        }

        private unsafe ModelOrganismDisplay AllocOrganism(BestiaryDesc desc, int index) {
            ModelOrganismDisplay display = m_OrganismPool.Alloc();
            display.Initialize(desc, index, m_OrganismInterventionDelegate);
            m_OrganismMap.Add(desc.Id(), display);
            m_SolverState.Positions[index] = new Vector2(RNG.Instance.NextFloat(-2, 2), RNG.Instance.NextFloat(-2, 2));
            m_SolverState.Forces[index] = default;
            m_SolverState.ConnectionMasks[index] = 0;
            return display;
        }

        private unsafe void GenerateConnection(BFBase fact, BestiaryDesc owner, BestiaryDesc target, BFDiscoveredFlags flags) {
            int indexA = m_OrganismMap[target.Id()].Index;
            int indexB = m_OrganismMap[owner.Id()].Index;
            m_SolverState.ConnectionMasks[indexA] |= (1u << indexB);
            m_SolverState.ConnectionMasks[indexB] |= (1u << indexA);

            ModelConnectionDisplay connection = m_ConnectionPool.Alloc();
            connection.Fact = fact;
            connection.IndexA = indexA;
            connection.IndexB = indexB;
            connection.Texture.texture = flags == BFDiscoveredFlags.All ? m_FullLineTexture : m_PartialLineTexture;
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
                distAB -= m_PositionScale * m_ConnectionOffsetFactor;
                display.Transform.SetSizeDelta(distAB, Axis.X);
                display.Transform.SetAnchorPos(centerAB);
                display.Transform.SetRotation(Mathf.Atan2(vecAB.y, vecAB.x) * Mathf.Rad2Deg, Axis.Z, Space.Self);
                display.Transform.SetAsFirstSibling();
            }
        }

        private unsafe void PositionOrganism(ModelOrganismDisplay display, ref GraphSolverState solverState, int index) {
            display.Transform.SetAnchorPos(m_SolverState.Positions[index] * m_PositionScale, Axis.XY);
        }

        private Vector2 GetPositionInv(ModelWaterPropertyDisplay display) {
            return display.GetComponent<RectTransform>().anchoredPosition / m_PositionScale;
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
                vecAB.y *= 1.3f;
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
            bool canSee = save.HasUpgrade(ItemIds.WaterModeling);
            m_LightProperty.gameObject.SetActive(canSee);
            m_TemperatureProperty.gameObject.SetActive(canSee);
            m_PHProperty.gameObject.SetActive(canSee);
            m_OxygenProperty.gameObject.SetActive(canSee);
            m_CarbonDioxideProperty.gameObject.SetActive(canSee);
        }

        private void SyncEnvironmentChemistry(WaterPropertyBlockF32 environment) {
            m_LightProperty.SetValue(environment.Light);
            m_TemperatureProperty.SetValue(environment.Temperature);
            m_PHProperty.SetValue(environment.PH);
            m_OxygenProperty.SetValue(environment.Oxygen);
            m_CarbonDioxideProperty.SetValue(environment.CarbonDioxide);
        }

        private ModelOrganismDisplay.AddRemoveResult OnOrganismRequestAddRemove(BestiaryDesc organism, int sign) {
            bool changedTarget = Ref.Replace(ref m_State.Simulation.Intervention.Target, organism);
            if (changedTarget) {
                m_State.Simulation.Intervention.Amount = 0;
            }

            BFBody body = organism.FactOfType<BFBody>();

            uint startingPopulation = m_State.Simulation.GetInterventionStartingPopulation(organism);
            int adjust = (int) (m_State.Simulation.Intervention.Amount + sign * body.PopulationSoftIncrement);
            long next = startingPopulation + adjust;
            
            if (next < 0) {
                next = 0;
            } else if (adjust > 0 && next > body.PopulationSoftCap) {
                next = body.PopulationSoftCap;
            } else if (adjust > -body.PopulationSoftIncrement && adjust < body.PopulationSoftIncrement) {
                next = startingPopulation;
            }

            adjust = (int) (next - startingPopulation);
            if (m_State.Simulation.Intervention.Amount != adjust) {
                m_State.Simulation.Intervention.Amount = adjust;
                if (adjust == 0 && !m_State.Simulation.IsInterventionNewOrganism()) {
                    changedTarget = true;
                    m_State.Simulation.Intervention.Target = null;
                } else if (!changedTarget) {
                    m_State.Simulation.DispatchInterventionUpdate();
                }
            }

            if (changedTarget) {
                m_State.Simulation.DispatchInterventionUpdate();
                UpdateInterventionControls();
            }

            ModelOrganismDisplay.AddRemoveResult result;
            result.CanAdd = adjust < 0 || next + body.PopulationSoftIncrement <= body.PopulationSoftCap;
            result.CanRemove = next - body.PopulationSoftIncrement >= 0;
            result.DifferenceValue = adjust;

            InterveneUpdateData data;
            data.Organism = organism.name;
            data.DifferenceValue = result.DifferenceValue;
            Services.Events.Dispatch(ModelingConsts.Event_Intervene_Update, data);

            return result;
        }

        private void OnPhaseChanged(ModelPhases prev, ModelPhases current) {
            if (current == ModelPhases.Intervene) {
                EnableIntervention();
            } else if (prev == ModelPhases.Intervene) {
                DisableIntervention();
            }
        }

        private void UpdateInterventionControls() {
            if (m_State.Phase != ModelPhases.Intervene) {
                return;
            }

            if (m_State.Simulation.Intervention.Target == null) {
                foreach(var display in m_OrganismPool.ActiveObjects) {
                    if (m_State.Conceptual.SimulatedEntities.Contains(display.Organism)) {
                        display.EnableIntervention(GetDesiredState(display.Organism, 0));
                    } else {
                        display.DisableIntervention();
                    }
                }
            } else {
                foreach(var display in m_OrganismPool.ActiveObjects) {
                    if (display.Organism == m_State.Simulation.Intervention.Target) {
                        display.EnableIntervention(GetDesiredState(display.Organism, m_State.Simulation.Intervention.Amount));
                    } else {
                        display.DisableIntervention();
                    }
                }
            }
        }

        private ModelOrganismDisplay.AddRemoveResult GetDesiredState(BestiaryDesc organism, int adjust) {
            BFBody body = organism.FactOfType<BFBody>();

            uint startingPopulation = m_State.Simulation.GetInterventionStartingPopulation(organism);
            long next = startingPopulation + adjust;

            ModelOrganismDisplay.AddRemoveResult result;
            result.CanAdd = adjust < 0 || next + body.PopulationSoftIncrement <= body.PopulationSoftCap;
            result.CanRemove = next - body.PopulationSoftIncrement >= 0;
            result.DifferenceValue = adjust;
            return result;
        }
    }

    public struct InterveneUpdateData
    {
        public string Organism;
        public int DifferenceValue;
    }
}
