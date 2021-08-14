using System;
using System.Collections;
using Aqua;
using Aqua.Cameras;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProtoAqua.ExperimentV2
{
    public class AvailableTanksView : MonoBehaviour, ISceneLoadHandler, ISceneOptimizable
    {
        #region Inspector

        [SerializeField, Required] private CameraPose m_Pose = null;
        [SerializeField, Required] private PointerListener m_BackButton = null;
        [SerializeField, HideInInspector] private SelectableTank[] m_Tanks = null;
        [SerializeField, Required] private CanvasGroup m_ExitSceneButtonGroup = null;

        #endregion // Inspector

        [NonSerialized] private SelectableTank m_SelectedTank;
        [NonSerialized] private Routine m_TankTransitionAnim;

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

            WorldUtils.ListenForLayerMask(inTank.WaterCollider, GameLayers.Critter_Mask, (c) => OnWaterEnter(inTank, c), null);
        }

        static private IEnumerator SelectTankTransition(SelectableTank inTank)
        {
            inTank.Interface.enabled = true;
            inTank.InterfaceFader.alpha = 0;
            inTank.InterfaceRaycaster.Override = null;

            Services.Input.PauseAll();
            switch(inTank.CurrentAvailability)
            {
                case TankAvailability.Available:
                    {
                        yield return Routine.Combine(
                            Services.Camera.MoveToPose(inTank.CameraPose, 0.2f, Curve.Smooth),
                            inTank.InterfaceFader.FadeTo(1, 0.2f)
                        );
                        break;
                    }

                default:
                    {
                        yield return Services.Camera.MoveToPose(inTank.CameraPose, 0.2f, Curve.Smooth);
                        break;
                    }
            }
            Services.Input.ResumeAll();
        }

        static private IEnumerator DeselectTankTransition(SelectableTank inTank, CameraPose inReturningPose)
        {
            inTank.InterfaceRaycaster.Override = false;

            Services.Input.PauseAll();
            yield return Routine.Combine(
                Services.Camera.MoveToPose(inReturningPose, 0.2f, Curve.Smooth),
                inTank.InterfaceFader.FadeTo(0, 0.2f)
            );
            inTank.Interface.enabled = false;

            Services.Input.ResumeAll();
        }

        static private void OnWaterEnter(SelectableTank inTank, Collider2D inCreature)
        {
            Vector3 critterPosition = inCreature.transform.position;
            Vector3 splashPosition;
            splashPosition.x = critterPosition.x;
            splashPosition.z = critterPosition.z;

            ActorInstance actor = inCreature.GetComponentInParent<ActorInstance>();
            actor.InWater = true;

            switch(actor.Definition.Spawning.SpawnLocation)
            {
                case ActorDefinition.SpawnPositionId.Top:
                case ActorDefinition.SpawnPositionId.Anywhere:
                    {
                        splashPosition.y = inTank.Bounds.max.y;
                        Services.Audio.PostEvent("tank_water_splash");
                        break;
                    }

                case ActorDefinition.SpawnPositionId.Bottom:
                    {
                        splashPosition.y = inTank.Bounds.min.y;
                        Services.Audio.PostEvent("tank_water_splash");
                        break;
                    }
            }

            // TODO: Splash
        }

        #endregion // Tank Anims

        #region Handlers

        private void OnTankClicked(PointerEventData inTankPointer)
        {
            PointerListener.TryGetComponentUserData<SelectableTank>(inTankPointer, out SelectableTank tank);
            
            m_SelectedTank = tank;
            DeactivateTankClickHandlers();
            m_BackButton.gameObject.SetActive(true);

            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, tank.Type);

            m_SelectedTank.ActivateMethod?.Invoke();
            Routine.Start(this, m_ExitSceneButtonGroup.Hide(0.2f, false));
            m_TankTransitionAnim.Replace(this, SelectTankTransition(tank)).TryManuallyUpdate(0);
        }

        private void OnBackClicked(PointerEventData inTankPointer)
        {
            Assert.NotNull(m_SelectedTank);
            if (m_SelectedTank.CanDeactivate != null && !m_SelectedTank.CanDeactivate())
                return;
            
            m_SelectedTank.DeactivateMethod?.Invoke();
            m_TankTransitionAnim.Replace(this, DeselectTankTransition(m_SelectedTank, m_Pose)).TryManuallyUpdate(0);
            m_SelectedTank = null;

            m_BackButton.gameObject.SetActive(false);
            ActivateTankClickHandlers();
            Routine.Start(this, m_ExitSceneButtonGroup.Show(0.2f, true));
        }

        #endregion // Handlers

        #region Interfaces

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            m_BackButton.onClick.AddListener(OnBackClicked);
            m_BackButton.gameObject.SetActive(false);

            foreach(var tank in m_Tanks)
            {
                InitializeTank(tank);
                tank.Clickable.onClick.AddListener(OnTankClicked);
            }
            Services.Camera.SnapToPose(m_Pose);
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_Tanks = FindObjectsOfType<SelectableTank>();
            foreach(var tank in m_Tanks)
            {
                tank.Bounds = tank.BoundsCollider.bounds;
            }
        }

        #endif // UNITY_EDITOR
    
        #endregion // Interfaces
    }
}