using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace ProtoAqua.Observation
{
    public class PlayerROV : MonoBehaviour, ISceneLoadHandler
    {
        #region Types

        public struct InputData
        {
            public Vector2? Target;
            public Vector2 Offset;

            public bool UsePress;
            public bool UseHold;
            public bool UseRelease;
            
            public bool ToolMode;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private KinematicObject2D m_Kinematic = null;
        [SerializeField] private PlayerROVWorldUI m_WorldUI = null;
        [SerializeField] private PlayerROVInput m_Input = null;
        [SerializeField] private PlayerROVScanner m_Scanner = null;

        [Header("Movement Params")]

        [SerializeField] private float m_TargetVectorMaxDistance = 5;
        [SerializeField] private float m_TargetVectorMinDistance = 0.2f;
        [SerializeField] private float m_TargetVectorSpeed = 1;
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineOff = 2;

        [Header("Camera Params")]

        [SerializeField] private float m_CameraForwardLook = 1;
        [SerializeField] private float m_CameraForwardLookWeight = 0.5f;
        [SerializeField] private float m_CameraForwardLookNoMove = 1;
        
        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private bool m_Moving;
        [NonSerialized] private AudioHandle m_EngineSound;
        [NonSerialized] private InputData m_LastInputData;

        [NonSerialized] private CameraConstraints.Hint m_VelocityHint;
        [NonSerialized] private CameraConstraints.Hint m_MouseHint;
        [NonSerialized] private CameraConstraints.Drift m_CameraDrift;

        private void Start()
        {
            this.CacheComponent(ref m_Transform);
            ObservationServices.Audio.PostEvent("underwater_loop");

            SetEngineState(false, true);

            m_VelocityHint = ObservationServices.Camera.AddHint("Player Velocity Lead");
            m_MouseHint = ObservationServices.Camera.AddHint("Player Mouse Lead");

            m_CameraDrift = ObservationServices.Camera.AddDrift("Ocean Drift");
            m_CameraDrift.Distance = new Vector2(0.1f, 0.1f);
            m_CameraDrift.Offset = new Vector2(RNG.Instance.NextFloat(), RNG.Instance.NextFloat());
            m_CameraDrift.Period = new Vector2(11, 7);
        }

        private void FixedUpdate()
        {
            CheckInput();
        }

        private void LateUpdate()
        {
            m_Kinematic.Properties.ApplyLimits();

            if (m_EngineSound.Exists())
            {
                m_EngineSound.SetPitch(Mathf.Clamp01(m_Kinematic.Properties.Velocity.magnitude / m_Kinematic.Properties.MaxSpeed));
            }

            m_VelocityHint.PositionAt(m_Transform, m_Kinematic.Properties.Velocity * (m_Moving ? m_CameraForwardLook : m_CameraForwardLookNoMove));
            m_VelocityHint.SetWeight(m_CameraForwardLookWeight);

            if (m_Moving)
            {
                float amt = Mathf.Clamp01(m_LastInputData.Offset.magnitude / m_TargetVectorMaxDistance);
                m_WorldUI.ShowMoveArrow(m_LastInputData.Offset, amt);
            }
            else if (m_LastInputData.ToolMode)
            {
                m_WorldUI.ShowScan(m_LastInputData.Offset, m_Scanner.CurrentTarget() != null);
            }
            else
            {
                m_WorldUI.HideCursor();
            }

            if (m_LastInputData.Target.HasValue)
            {
                Vector2 offset = m_LastInputData.Offset;
                if (offset.magnitude > m_TargetVectorMaxDistance)
                {
                    offset.Normalize();
                    offset *= m_TargetVectorMaxDistance;
                }
                m_MouseHint.PositionAt(m_Transform, offset);
                m_MouseHint.SetWeight(m_CameraForwardLookWeight);
            }
            else
            {
                m_MouseHint.SetWeight(0);
            }
        }

        private void CheckInput()
        {
            Transform lockOn = GetLockOn();

            m_Input.GenerateInput(m_Transform, lockOn, out m_LastInputData);

            if (m_LastInputData.ToolMode)
            {
                UpdateTool();
            }
            else
            {
                UpdateMove();
            }
        }

        private Transform GetLockOn()
        {
            ScannableRegion currentScan = m_Scanner.CurrentTarget();
            if (currentScan && currentScan.isActiveAndEnabled)
                return currentScan.transform;

            return null;
        }

        private void UpdateTool()
        {
            SetEngineState(false);

            m_Scanner.Enable();
            m_Scanner.UpdateScan(m_LastInputData);
        }

        private void UpdateMove()
        {
            m_Scanner.Disable();

            if (m_LastInputData.UseHold)
            {
                Vector2 vector = m_LastInputData.Offset;
                float dist = vector.magnitude;
                vector.Normalize();

                if (dist > m_TargetVectorMinDistance)
                {
                    SetEngineState(true);
                    float desiredSpeed = m_Kinematic.Properties.MaxSpeed * Mathf.Clamp01(dist / m_TargetVectorMaxDistance);
                    float speedChange = desiredSpeed - m_Kinematic.Properties.Velocity.magnitude;
                    if (speedChange > 0)
                    {
                        vector *= speedChange * Routine.DeltaTime * m_TargetVectorSpeed;
                        m_Kinematic.Properties.Velocity += vector;
                    }
                }
                else
                {
                    SetEngineState(false);
                }
            }
            else
            {
                SetEngineState(false);
            }
        }

        private void SetEngineState(bool inbOn, bool inbForce = false)
        {
            if (!inbForce && m_Moving == inbOn)
                return;

            m_Moving = inbOn;
            if (inbOn)
            {
                m_EngineSound = ObservationServices.Audio.PostEvent("rov_engine_loop");
                m_EngineSound.SetVolume(0).SetVolume(1, 0.25f);

                m_Kinematic.Properties.Drag = m_DragEngineOn;
            }
            else
            {
                m_EngineSound.Stop(0.25f);

                m_Kinematic.Properties.Drag = m_DragEngineOff;
            }
        }

        void ISceneLoadHandler.OnSceneLoad(Scene inScene, object inContext)
        {
            Services.UI.HideLoadingScreen();
            Services.Script.StartNode("testScene.sceneStart");
        }
    }
}