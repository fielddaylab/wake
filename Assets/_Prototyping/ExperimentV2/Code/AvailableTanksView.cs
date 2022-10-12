using System;
using System.Collections;
using Aqua;
using Aqua.Cameras;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2 {
    public class AvailableTanksView : MonoBehaviour, ISceneLoadHandler, IBaked, ISceneUnloadHandler
    {
        static private AvailableTanksView s_Instance;

        #region Inspector

        [SerializeField, HideInInspector] private SelectableTank[] m_Tanks = null;
        [SerializeField, Required] private SelectableTank m_StartingTank = null;
        [SerializeField, Required] private TankWaterSystem m_WaterSystem = null;
        [SerializeField, Required] private Transform m_GuideTransform = null;

        #endregion // Inspector

        [NonSerialized] private SelectableTank m_SelectedTank;
        [NonSerialized] private Routine m_TankShutdownRoutine;
        [NonSerialized] private Routine m_TankEnterTransitionAnim;

        #region Tank Anims

        static private void InitializeTank(SelectableTank inTank)
        {
            inTank.Interface.enabled = false;
            inTank.InterfaceRaycaster.Override = false;
            inTank.InterfaceFader.alpha = 0;
            inTank.DefaultWaterColor = inTank.WaterColor.Color;

            for(int i = 0; i < inTank.Lights.Length; i++) {
                inTank.Lights[i].enabled = false;
            }

            inTank.ActorBehavior.Initialize();
        }

        static private IEnumerator SelectTankTransition(SelectableTank inTank)
        {
            inTank.Interface.enabled = true;
            inTank.InterfaceFader.alpha = 0;
            inTank.InterfaceRaycaster.Override = null;

            using(Script.DisableInput()) {
                SelectableTank.SetLights(inTank, true);
                yield return Routine.Combine(
                    Services.Camera.MoveToPose(inTank.CameraPose, 0.2f, Curve.Smooth),
                    inTank.InterfaceFader.FadeTo(1, 0.2f)
                );
            }
        }

        static private IEnumerator DeselectTankTransition(SelectableTank inTank)
        {
            inTank.InterfaceRaycaster.Override = false;

            inTank.WaterAudioLoop.Stop();

            using(Script.DisableInput()) {
                yield return inTank.InterfaceFader.FadeTo(0, 0.2f);
                inTank.Interface.enabled = false;
                SelectableTank.SetLights(inTank, false);
                SelectableTank.Reset(inTank);
                inTank.DeactivateMethod?.Invoke();
            }
        }

        #endregion // Tank Anims

        #region Handlers

        private void OnTankNavigated(SelectableTank tank) {
            // Exit prev tank (if applicable)
            if (m_SelectedTank != null) {
                if (m_SelectedTank.CanDeactivate != null && !m_SelectedTank.CanDeactivate())
                    return;

                m_SelectedTank.CurrentState &= ~TankState.Selected;

                m_TankShutdownRoutine.Replace(this, DeselectTankTransition(m_SelectedTank)).Tick();
                // de-activate nav arrows
                m_SelectedTank.NavArrowParent.SetActive(false);
            }

            // Enter new tank

            m_SelectedTank = tank;

            SelectableTank.Reset(m_SelectedTank, true);

            for(int i = 0; i < m_SelectedTank.Lights.Length; i++) {
                m_SelectedTank.Lights[i].enabled = true;
            }

            m_WaterSystem.SetActiveTank(tank);

            Services.Events.Dispatch(ExperimentEvents.ExperimentView, tank.Type);

            using (var table = TempVarTable.Alloc()) {
                table.Set("tankType", tank.Type.ToString());
                table.Set("tankId", tank.Id);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentTankViewed, table);
            }

            m_SelectedTank.CurrentState |= TankState.Selected;
            m_SelectedTank.ActivateMethod?.Invoke();

            // activate nav arrows
            m_SelectedTank.NavArrowParent.SetActive(true);

            m_TankEnterTransitionAnim.Replace(this, SelectTankTransition(tank)).Tick();
        }

        #endregion // Handlers

        #region Interfaces

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            s_Instance = this;
            foreach(var tank in m_Tanks)
            {
                InitializeTank(tank);
                m_WaterSystem.InitializeTank(tank);
                foreach (var arrow in tank.NavArrows) {
                    arrow.Button.onClick.AddListener(delegate { OnTankNavigated(arrow.DestTank); });
                    arrow.Button.onClick.AddListener(delegate { Routine.Start( UpdateGuidePosition.MoveGuide( m_GuideTransform, arrow.DestTank.GuideTarget.transform ) ); });
                }
            }

            // Navigate to the initial tank

            OnTankNavigated(m_StartingTank);
            Services.Camera.SnapToPose(m_StartingTank.CameraPose);
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext)
        {
            Services.Events?.DeregisterAll(this);

            s_Instance = null;
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            m_Tanks = FindObjectsOfType<SelectableTank>();
            foreach(var tank in m_Tanks)
            {
                tank.Bounds = tank.BoundsCollider.bounds;

                Bounds waterBounds = PhysicsUtils.GetLocalBounds(tank.WaterTrigger);
                waterBounds.center += tank.WaterTrigger.transform.localPosition;
                tank.WaterRect = Geom.BoundsToRect(waterBounds);
            }

            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // Interfaces

        #region Leaf

        [LeafMember("ExperimentHasCritter"), UnityEngine.Scripting.Preserve]
        static private bool LeafTankHasCritter(StringHash32 inCritterId)
        {
            Assert.NotNull(s_Instance, "Cannot call experiment leaf methods when outside of experiment room");
            SelectableTank tank = s_Instance.m_SelectedTank;
            if (tank != null && tank.HasCritter != null)
            {
                return tank.HasCritter(inCritterId);
            }
            return false;
        }

        [LeafMember("ExperimentHasEnv"), UnityEngine.Scripting.Preserve]
        static private bool LeafTankHasEnvironment(StringHash32 inEnvId)
        {
            Assert.NotNull(s_Instance, "Cannot call experiment leaf methods when outside of experiment room");
            SelectableTank tank = s_Instance.m_SelectedTank;
            if (tank != null && tank.HasEnvironment != null)
            {
                return tank.HasEnvironment(inEnvId);
            }
            return false;
        }

        [LeafMember("ExperimentType"), UnityEngine.Scripting.Preserve]
        static private StringHash32 LeafTankType()
        {
            Assert.NotNull(s_Instance, "Cannot call experiment leaf methods when outside of experiment room");
            SelectableTank tank = s_Instance.m_SelectedTank;
            if (tank != null)
                return tank.Type.ToString();
            return null;
        }

        [LeafMember("ExperimentViewed"), UnityEngine.Scripting.Preserve]
        static private bool LeafTankViewed(TankType inType)
        {
            Assert.NotNull(s_Instance, "Cannot call experiment leaf methods when outside of experiment room");
            SelectableTank tank = s_Instance.m_SelectedTank;
            if (tank != null)
                return tank.Type == inType;
            return false;
        }

        [LeafMember("ExperimentIsRunning"), UnityEngine.Scripting.Preserve]
        static private bool LeafTankIsRunning(TankType inTankType = TankType.Unknown)
        {
            Assert.NotNull(s_Instance, "Cannot call experiment leaf methods when outside of experiment room");
            SelectableTank tank = s_Instance.m_SelectedTank;
            if (tank != null)
            {
                if (inTankType != TankType.Unknown && tank.Type != inTankType)
                    return false;
                
                return (tank.CurrentState & TankState.Running) != 0;
            }
            return false;
        }

        #endregion // Leaf
    }
}