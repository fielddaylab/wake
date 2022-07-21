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

        [SerializeField, Required] private CameraPose m_Pose = null;
        [SerializeField, HideInInspector] private SelectableTank[] m_Tanks = null;
        [SerializeField, Required] private SelectableTank m_StartingTank = null;
        [SerializeField, Required] private CanvasGroup m_ExitSceneButtonGroup = null;
        [SerializeField, Required] private CanvasGroup m_ExitTankButtonGroup = null;
        [SerializeField, Required] private Button m_ExitTankButton = null;
        [SerializeField, Required] private TankWaterSystem m_WaterSystem = null;

        #endregion // Inspector

        [NonSerialized] private SelectableTank m_SelectedTank;
        [NonSerialized] private Routine m_TankExitTransitionAnim;
        [NonSerialized] private Routine m_TankEnterTransitionAnim;
        [NonSerialized] private Routine m_ExitTankButtonAnimation;

        private void ActivateTankClickHandlers()
        {
            foreach(var tank in m_Tanks)
            {
                tank.Clickable.gameObject.SetActive(true);
            }
        }

        private void DeactivateTankClickHandlers()
        {
            foreach(var tank in m_Tanks)
            {
                tank.Clickable.gameObject.SetActive(false);
            }
        }

        #region Tank Anims

        static private void InitializeTank(SelectableTank inTank)
        {
            inTank.Interface.enabled = false;
            inTank.InterfaceRaycaster.Override = false;
            inTank.InterfaceFader.alpha = 0;
            inTank.Clickable.UserData = inTank;
            inTank.DefaultWaterColor = inTank.WaterColor.Color;

            inTank.ActorBehavior.Initialize();
        }

        static private IEnumerator SelectTankTransition(SelectableTank inTank)
        {
            inTank.Interface.enabled = true;
            inTank.InterfaceFader.alpha = 0;
            inTank.InterfaceRaycaster.Override = null;

            using(Script.DisableInput()) {
                yield return Routine.Combine(
                    Services.Camera.MoveToPose(inTank.CameraPose, 0.2f, Curve.Smooth),
                    inTank.InterfaceFader.FadeTo(1, 0.2f)
                );
            }
        }

        static private IEnumerator DeselectTankTransition(SelectableTank inTank, CameraPose inReturningPose)
        {
            inTank.InterfaceRaycaster.Override = false;

            inTank.WaterAudioLoop.Stop();

            using(Script.DisableInput()) {
                yield return Routine.Combine(
                    Services.Camera.MoveToPose(inReturningPose, 0.2f, Curve.Smooth),
                    inTank.InterfaceFader.FadeTo(0, 0.2f)
                );
                inTank.Interface.enabled = false;
                SelectableTank.Reset(inTank);
                inTank.DeactivateMethod?.Invoke();
            }
        }

        #endregion // Tank Anims

        #region Handlers

        private void OnTankClicked(PointerEventData inTankPointer)
        {
            PointerListener.TryGetComponentUserData<SelectableTank>(inTankPointer, out SelectableTank tank);
            
            OnTankNavigated(tank);
        }

        private void OnTankNavigated(SelectableTank tank) {
            // Exit prev tank (if applicable)
            if (m_SelectedTank != null) {
                if (m_SelectedTank.CanDeactivate != null && !m_SelectedTank.CanDeactivate())
                    return;

                m_SelectedTank.CurrentState &= ~TankState.Selected;

                m_TankExitTransitionAnim.Replace(this, DeselectTankTransition(m_SelectedTank, m_Pose)).Tick();
                // de-activate nav arrows
                m_SelectedTank.NavArrowParent.SetActive(false);

                Routine.Start(this, m_ExitSceneButtonGroup.Show(0.2f, true));
                m_ExitTankButtonAnimation.Replace(this, m_ExitTankButtonGroup.Hide(0.2f, false));

                // Services.Script.TriggerResponse(ExperimentTriggers.ExperimentTankExited);
            }

            // Enter new tank

            m_SelectedTank = tank;

            SelectableTank.Reset(m_SelectedTank, true);

            DeactivateTankClickHandlers();
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

            Routine.Start(this, m_ExitSceneButtonGroup.Hide(0.2f, false));
            m_ExitTankButtonAnimation.Replace(this, m_ExitTankButtonGroup.Show(0.2f, true));
            m_TankEnterTransitionAnim.Replace(this, SelectTankTransition(tank)).Tick();
        }

        private void OnBackClicked()
        {
            Assert.NotNull(m_SelectedTank);
            if (m_SelectedTank.CanDeactivate != null && !m_SelectedTank.CanDeactivate())
                return;
            
            m_SelectedTank.CurrentState &= ~TankState.Selected;

            m_TankExitTransitionAnim.Replace(this, DeselectTankTransition(m_SelectedTank, m_Pose)).Tick();

            // de-activate nav arrows
            m_SelectedTank.NavArrowParent.SetActive(false);

            m_SelectedTank = null;

            ActivateTankClickHandlers();
            Routine.Start(this, m_ExitSceneButtonGroup.Show(0.2f, true));
            m_ExitTankButtonAnimation.Replace(this, m_ExitTankButtonGroup.Hide(0.2f, false));

            Services.Script.TriggerResponse(ExperimentTriggers.ExperimentTankExited);
        }

        private void OnExperimentStart(TankType inTankType) {
            if (inTankType == TankType.Stress) {
                return;
            }

            m_ExitTankButtonAnimation.Replace(this, m_ExitTankButtonGroup.Hide(0.2f));
        }

        private void OnExperimentFinish(TankType inTankType) {
            if (inTankType == TankType.Stress) {
                return;
            }

            m_ExitTankButtonAnimation.Replace(this, m_ExitTankButtonGroup.Show(0.2f));
        }

        #endregion // Handlers

        #region Interfaces

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            s_Instance = this;
            foreach(var tank in m_Tanks)
            {
                InitializeTank(tank);
                m_WaterSystem.InitializeTank(tank);
                tank.Clickable.onClick.AddListener(OnTankClicked);
                foreach (var arrow in tank.NavArrows) {
                    arrow.Button.onClick.AddListener(delegate { OnTankNavigated(arrow.DestTank); });
                }
            }
            Services.Camera.SnapToPose(m_Pose);
            m_ExitTankButton.onClick.AddListener(OnBackClicked);

            Services.Events.Register<TankType>(ExperimentEvents.ExperimentBegin, OnExperimentStart, this)
                .Register<TankType>(ExperimentEvents.ExperimentEnded, OnExperimentFinish, this);

            // Navigate to the initial tank

            OnTankNavigated(m_StartingTank);
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext)
        {
            Services.Events?.DeregisterAll(this);

            s_Instance = null;
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
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