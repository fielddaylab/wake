using System;
using UnityEngine;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using Aqua;
using BeauUtil.Debugger;

namespace ProtoAqua.Observation
{
    public class PlayerROV : MonoBehaviour, ISceneLoadHandler
    {
        static public readonly StringHash32 Event_RequestToolSwitch = "PlayerROV::RequestToolSwitch";
        static public readonly StringHash32 Event_ToolSwitched = "PlayerROV::ToolSwitched";

        #region Types

        public struct InputData
        {
            public Vector2? Target;
            public Vector2 Offset;

            public bool UsePress;
            public bool UseHold;
        }

        public enum ToolId
        {
            Scanner,
            Tagger,

            NONE
        }

        public interface ITool
        {
            void Enable();
            void Disable();
            bool UpdateTool(in PlayerROV.InputData inInput);
            bool HasTarget();
            Vector3? GetTargetPosition();
        }

        private class NullTool : ITool
        {
            static public readonly NullTool Instance = new NullTool();

            public void Disable() { }
            public void Enable() { }

            public Vector3? GetTargetPosition() { return null; }
            public bool HasTarget() { return false; }
            public bool UpdateTool(in InputData inInput) { return false; }
        }

        #endregion // Types

        #region Inspector

        [SerializeField, Required] private KinematicObject2D m_Kinematic = null;
        [SerializeField, Required] private PlayerROVWorldUI m_WorldUI = null;
        [SerializeField, Required] private PlayerROVInput m_Input = null;
        [SerializeField, Required] private PlayerROVScanner m_Scanner = null;
        [SerializeField, Required] private PlayerROVTagger m_Tagger = null;
        [SerializeField, Required] private Transform m_Renderer = null;

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
        [SerializeField] private float m_CameraZoomTool = 1.1f;
        
        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private bool m_Moving;
        [NonSerialized] private AudioHandle m_EngineSound;
        [NonSerialized] private InputData m_LastInputData;

        [NonSerialized] private uint m_VelocityHint;
        [NonSerialized] private uint m_MouseHint;
        [NonSerialized] private uint m_CameraDriftHint;
        [NonSerialized] private ITool m_CurrentTool;
        [NonSerialized] private ToolId m_CurrentToolId = ToolId.NONE;

        private void Start()
        {
            this.CacheComponent(ref m_Transform);

            Services.Events.Register<ToolId>(Event_RequestToolSwitch, (toolId) => SetTool(toolId, false));

            SetEngineState(false, true);

            m_VelocityHint = Services.Camera.AddHint(m_Transform, 1, 0).Id;
            m_MouseHint = Services.Camera.AddHint(m_Transform, 1, m_CameraForwardLookWeight).Id;

            m_CameraDriftHint = Services.Camera.AddDrift(new Vector2(0.1f, 0.1f), new Vector2(11, 7), RNG.Instance.NextVector2()).Id;

            m_Input.OnInputDisabled.AddListener(OnInputDisabled);
            m_Input.OnInputEnabled.AddListener(OnInputEnabled);

            m_CurrentTool = NullTool.Instance;
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            if (Services.Data.Profile.Inventory.HasUpgrade("ROVScanner"))
            {
                SetTool(ToolId.Scanner, true);
            }
            else
            {
                SetTool(ToolId.NONE, true);
            }

            if (m_Input.IsInputEnabled)
                OnInputEnabled();
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void FixedUpdate()
        {
            CheckInput();
        }

        private void LateUpdate()
        {
            if (Services.State.IsLoadingScene())
                return;
            
            ref var velocityHintData = ref Services.Camera.FindHint(m_VelocityHint);
            ref var mouseHintData = ref Services.Camera.FindHint(m_MouseHint);

            KinematicMath2D.ApplyLimits(ref m_Kinematic.State, ref m_Kinematic.Config);

            if (m_EngineSound.Exists())
            {
                m_EngineSound.SetPitch(Mathf.Clamp01(m_Kinematic.State.Velocity.magnitude / m_Kinematic.Config.MaxSpeed));
            }

            velocityHintData.WeightOffset = m_CameraForwardLookWeight;
            velocityHintData.Offset = m_Kinematic.State.Velocity * (m_Moving ? m_CameraForwardLook : m_CameraForwardLookNoMove);

            if (m_Moving)
            {
                float amt = Mathf.Clamp01(m_LastInputData.Offset.magnitude / m_TargetVectorMaxDistance);
                m_WorldUI.ShowMoveArrow(m_LastInputData.Offset, amt);

                float scaleX = Math.Abs(m_Renderer.transform.localScale.x) * Math.Sign(m_LastInputData.Offset.x);
                m_Renderer.transform.SetScale(scaleX, Axis.X);
            }
            else if (m_CurrentTool.HasTarget())
            {
                m_WorldUI.ShowScan(m_LastInputData.Offset, true);
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

                mouseHintData.Offset = offset;
                mouseHintData.WeightOffset = m_CameraForwardLookWeight;
                mouseHintData.Zoom = m_CurrentTool.HasTarget() ? m_CameraZoomTool : 1f;
            }
            else
            {
                mouseHintData.WeightOffset = 0;
            }
        }

        private void CheckInput()
        {
            Vector3? lockOn = GetLockOn();

            m_Input.GenerateInput(m_Transform, lockOn, out m_LastInputData);

            if (m_Moving || !UpdateTool())
            {
                UpdateMove();
            }

            // Debug.LogFormat("[PlayerROV] Contact Below = {0}", PhysicsService.CheckSolid(m_Kinematic, Vector2.down * 0.05f, out Vector2 ignore));
        }

        private Vector3? GetLockOn()
        {
            Vector3? currentScanPos = m_CurrentTool.GetTargetPosition();
            if (currentScanPos.HasValue)
            {
                Vector3 pos = currentScanPos.Value;
                pos.z = m_Transform.position.z;
                return pos;
            }

            return null;
        }

        private bool UpdateTool()
        {
            if (m_CurrentTool.UpdateTool(m_LastInputData))
            {
                SetEngineState(false);
                return true;
            }

            return false;
        }

        private void OnInputEnabled()
        {
            m_CurrentTool.Enable();
        }

        private void OnInputDisabled()
        {
            m_CurrentTool.Disable();
        }

        private void UpdateMove()
        {
            if (m_LastInputData.UseHold)
            {
                Vector2 vector = m_LastInputData.Offset;
                float dist = vector.magnitude;
                vector.Normalize();

                if (dist > m_TargetVectorMinDistance)
                {
                    SetEngineState(true);
                    float desiredSpeed = m_Kinematic.Config.MaxSpeed * Mathf.Clamp01(dist / m_TargetVectorMaxDistance);
                    float speedChange = desiredSpeed - m_Kinematic.State.Velocity.magnitude;
                    if (speedChange > 0)
                    {
                        vector *= speedChange * Routine.DeltaTime * m_TargetVectorSpeed;
                        vector = PhysicsService.SmoothVelocity(vector);
                        
                        Vector2 collideNormal;
                        if (m_Kinematic.CheckSolid(vector, out collideNormal))
                        {
                            vector = PhysicsService.SmoothDeflect(vector, collideNormal);
                        }
                        
                        m_Kinematic.State.Velocity += vector;
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
                m_EngineSound = Services.Audio.PostEvent("rov_engine_loop");
                m_EngineSound.SetVolume(0).SetVolume(1, 0.25f);

                m_Kinematic.Config.Drag = m_DragEngineOn;
                Services.UI?.FindPanel<ScannerDisplay>()?.Hide();
                m_Scanner.HideCurrentToolView();
            }
            else
            {
                m_EngineSound.Stop(0.25f);

                m_Kinematic.Config.Drag = m_DragEngineOff;
            }
        }
    
        private bool SetTool(ToolId inTool, bool inbForce)
        {
            if (!inbForce && m_CurrentToolId == inTool)
                return false;

            if (m_CurrentTool != null)
            {
                m_CurrentTool.Disable();
            }

            m_CurrentToolId = inTool;
            m_CurrentTool = GetTool(inTool);

            if (m_CurrentTool != null && m_Input.IsInputEnabled)
                m_CurrentTool.Enable();

            Services.Events.Dispatch(Event_ToolSwitched, inTool);
            return true;
        }

        private ITool GetTool(ToolId inToolId)
        {
            switch(inToolId)
            {
                case ToolId.Scanner:
                    return m_Scanner;
                case ToolId.Tagger:
                    return m_Tagger;
                case ToolId.NONE:
                    return NullTool.Instance;
                default:
                    Assert.Fail("unknown toolid {0}", inToolId);
                    return null;
            }
        }
    }
}