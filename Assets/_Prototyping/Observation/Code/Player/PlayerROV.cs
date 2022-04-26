using System;
using UnityEngine;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using Aqua;
using BeauUtil.Debugger;
using Aqua.Scripting;
using Aqua.Character;
using Leaf.Runtime;
using System.Collections;
using BeauUtil.Variants;

namespace ProtoAqua.Observation
{
    public class PlayerROV : PlayerBody, ISceneLoadHandler
    {
        static public readonly TableKeyPair Var_LastFlashlightState = TableKeyPair.Parse("player:lastFlashlightState");

        static public readonly StringHash32 Event_RequestToolToggle = "PlayerROV::RequestToolToggle"; // ToolState
        static public readonly StringHash32 Event_RequestToolSwitch = "PlayerROV::RequestToolSwitch"; // tool id
        static public readonly StringHash32 Event_ToolSwitched = "PlayerROV::ToolSwitched"; // tool id

        #region Types

        public struct ToolState
        {
            public readonly ToolId Id;
            public readonly bool Active;

            public ToolState(ToolId id, bool active) {
                Id = id;
                Active = active;
            }
        }

        public enum ToolId
        {
            Scanner,
            Tagger,
            Flashlight,
            Microscope,

            NONE
        }

        [Flags]
        private enum PassiveUpgradeMask {
            Engine = 0x01,
            PropGuard = 0x02
        }

        static public StringHash32 ToolIdToItemId(ToolId id) {
            switch(id) {
                case ToolId.Scanner:
                    return ItemIds.ROVScanner;
                case ToolId.Tagger:
                    return ItemIds.ROVTagger;
                case ToolId.Flashlight:
                    return ItemIds.Flashlight;
                case ToolId.Microscope:
                    return ItemIds.Microscope;
                
                default:
                    return null;
            }
        }

        public interface ITool
        {
            bool IsEnabled();
            void Enable(PlayerBody inBody);
            void Disable();
            bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody);
            void UpdateActive();
            bool HasTarget();
            void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorldPosition, out Vector3? outCursorPosition);
        }

        private class NullTool : ITool
        {
            static public readonly NullTool Instance = new NullTool();

            public bool IsEnabled() { return true; }
            public void Disable() { }
            public void Enable(PlayerBody inBody) { }

            public void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorldPosition, out Vector3? outCursorPosition) {
                outWorldPosition = outCursorPosition = null;
            }
            public bool HasTarget() { return false; }
            public bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) { return false; }
            public void UpdateActive() { }
        }

        #endregion // Types

        #region Inspector

        [SerializeField, Required] private PlayerROVWorldUI m_WorldUI = null;
        [SerializeField, Required] private PlayerROVInput m_Input = null;
        [SerializeField, Required] private PlayerROVScanner m_Scanner = null;
        [SerializeField, Required] private PlayerROVTagger m_Tagger = null;
        [SerializeField, Required] private PlayerROVFlashlight m_Flashlight = null;
        [SerializeField, Required] private PlayerROVMicroscope m_Microscope = null;
        [SerializeField, Required] private PlayerROVAnimator m_Animator = null;
        [SerializeField, Required] private Collider2D m_TriggerCollider = null;

        [Header("Movement Params")]

        [SerializeField] private MovementFromOffset m_MovementParams = default(MovementFromOffset);
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineOff = 2;

        [Header("Camera Params")]

        [SerializeField] private float m_CameraForwardLook = 1;
        [SerializeField] private float m_CameraForwardLookWeight = 0.5f;
        [SerializeField] private float m_CameraForwardLookNoMove = 1;
        [SerializeField] private float m_CameraZoomTool = 1.1f;
        
        #endregion // Inspector

        [NonSerialized] private bool m_Moving;
        [NonSerialized] private AudioHandle m_EngineSound;
        [NonSerialized] private AudioHandle m_PowerEngineSound;
        [NonSerialized] private PlayerROVInput.InputData m_LastInputData;

        [NonSerialized] private uint m_VelocityHint;
        [NonSerialized] private uint m_MouseHint;
        [NonSerialized] private uint m_CameraDriftHint;
        [NonSerialized] private ITool m_CurrentTool;
        [NonSerialized] private ToolId m_CurrentToolId = ToolId.NONE;
        [NonSerialized] private PassiveUpgradeMask m_UpgradeMask = 0;
        [NonSerialized] private Routine m_StunRoutine;
        [NonSerialized] private int m_EngineRegionCount;
        [NonSerialized] private int m_SlowRegionCount;

        private void Start()
        {
            this.CacheComponent(ref m_Transform);

            Services.Events.Register<ToolId>(Event_RequestToolSwitch, (toolId) => SwitchTool(toolId, false), this)
                .Register<ToolState>(Event_RequestToolToggle, (s) => SetToolState(s.Id, s.Active, false), this)
                .Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryUpdated, this);

            SetEngineState(false, true);

            m_VelocityHint = Services.Camera.AddHint(m_Transform, 1, 0).Id;
            m_MouseHint = Services.Camera.AddHint(m_Transform, 1, m_CameraForwardLookWeight).Id;

            m_CameraDriftHint = Services.Camera.AddDrift(new Vector2(0.1f, 0.1f), new Vector2(11, 7), RNG.Instance.NextVector2()).Id;

            m_Input.OnInputDisabled.AddListener(OnInputDisabled);
            m_Input.OnInputEnabled.AddListener(OnInputEnabled);

            m_CurrentTool = NullTool.Instance;

            WorldUtils.ListenForLayerMask(m_TriggerCollider, GameLayers.PlayerTrigger_Mask, OnEnterTrigger, OnExitTrigger);
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            ToolId lastToolId = ToolId.NONE;
            if (Save.Inventory.HasUpgrade(ItemIds.ROVScanner)) {
                lastToolId = ToolId.Scanner;
            }

            UpdateUpgradeMask();

            SwitchTool(lastToolId, true);

            SetToolState(ToolId.Flashlight, Script.ReadVariable(Var_LastFlashlightState).AsBool(), true);
            SetToolState(ToolId.Microscope, false, true);

            if (m_Input.IsInputEnabled)
                OnInputEnabled();
        }

        private void OnDestroy()
        {
            m_EngineSound.Stop();
            m_PowerEngineSound.Stop();
            Services.Events?.DeregisterAll(this);
        }

        protected override void Tick(float inDeltaTime)
        {
            Vector3? lockOn = GetLockOn();

            PlayerBodyStatus status = PlayerBodyStatus.Normal;
            if (m_StunRoutine) {
                status |= PlayerBodyStatus.Stunned;
            }
            if (m_EngineRegionCount > 0 && (m_UpgradeMask & PassiveUpgradeMask.Engine) != 0) {
                status |= PlayerBodyStatus.PowerEngineEngaged;
            }
            if (m_SlowRegionCount > 0) {
                status |= PlayerBodyStatus.Slowed;
            }

            m_BodyStatus = status;

            m_Input.GenerateInput(m_Transform, lockOn, status, out m_LastInputData);

            if (m_Moving || !UpdateTool())
            {
                UpdateMove(inDeltaTime);
            }

            m_CurrentTool.UpdateActive();
        }

        private void LateUpdate()
        {
            if (Script.IsLoading)
                return;
            
            ref var velocityHintData = ref Services.Camera.FindHint(m_VelocityHint);
            ref var mouseHintData = ref Services.Camera.FindHint(m_MouseHint);

            KinematicMath2D.ApplyLimits(ref m_Kinematics.State, ref m_Kinematics.Config);

            if (m_EngineSound.Exists())
            {
                m_EngineSound.SetPitch(Mathf.Clamp01(m_Kinematics.State.Velocity.magnitude / m_Kinematics.Config.MaxSpeed));
            }

            velocityHintData.WeightOffset = m_CameraForwardLookWeight;
            velocityHintData.Offset = m_Kinematics.State.Velocity * (m_Moving ? m_CameraForwardLook : m_CameraForwardLookNoMove);

            if (m_Moving && m_LastInputData.UseHold && !m_LastInputData.Keyboard.KeyDown)
            {
                float amt = m_LastInputData.Mouse.NormalizedOffset.magnitude;
                m_WorldUI.ShowMoveArrow(m_LastInputData.Mouse.RawOffset, amt);
            }
            else if (m_CurrentTool.HasTarget())
            {
                m_WorldUI.ShowScan(m_LastInputData.Mouse.RawOffset, true);
            }
            else
            {
                m_WorldUI.HideCursor();
            }

            if (m_LastInputData.Mouse.Target.HasValue)
            {
                mouseHintData.Offset = m_LastInputData.Mouse.ClampedOffset;
                mouseHintData.WeightOffset = m_CameraForwardLookWeight;
                mouseHintData.Zoom = m_CurrentTool.HasTarget() ? m_CameraZoomTool : 1f;
            }
            else
            {
                mouseHintData.WeightOffset = 0;
            }

            UpdateAnims();
        }

        private void UpdateAnims() {
            PlayerROVAnimator.InputState animState;
            animState.Position = m_Transform.position;
            animState.Moving = m_Moving;
            animState.UsingTool = m_CurrentTool.HasTarget();
            animState.NormalizedLook = m_LastInputData.Mouse.NormalizedOffset;
            m_CurrentTool.GetTargetPosition(false, out animState.LookTarget, out var _);
            animState.NormalizedMove = m_LastInputData.MoveVector;
            animState.Status = m_BodyStatus;
            m_Animator.Process(animState);
        }

        private Vector3? GetLockOn()
        {
            m_CurrentTool.GetTargetPosition(true, out var _, out Vector3? currentScanPos);
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
            if (m_CurrentTool.UpdateTool(m_LastInputData, m_Kinematics.State.Velocity, this))
            {
                SetEngineState(false);
                return true;
            }

            return false;
        }

        private void OnInputEnabled()
        {
            m_CurrentTool.Enable(this);
        }

        private void OnInputDisabled()
        {
            m_CurrentTool.Disable();
        }

        private void UpdateMove(float inDeltaTime)
        {
            if (m_LastInputData.Move)
            {
                float dist = m_LastInputData.MoveVector.magnitude;
                
                if (dist > 0)
                {
                    SetEngineState(true);
                    m_MovementParams.Apply(m_LastInputData.MoveVector, m_Kinematics, inDeltaTime);
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

            if (!m_Moving && !m_Input.IsInputEnabled) {
                m_Kinematics.enabled = false;
            } else {
                m_Kinematics.enabled = true;
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

                m_Kinematics.Config.Drag = m_DragEngineOn;
                Services.UI?.FindPanel<ScannerDisplay>()?.Hide();
            }
            else
            {
                m_EngineSound.Stop(0.25f);

                m_Kinematics.Config.Drag = m_DragEngineOff;
            }
        }
    
        private bool SwitchTool(ToolId inTool, bool inbForce)
        {
            if (!inbForce && m_CurrentToolId == inTool)
                return false;

            if (m_CurrentTool != null && m_Input.IsInputEnabled)
                m_CurrentTool.Disable();

            m_CurrentToolId = inTool;
            m_CurrentTool = GetTool(inTool);

            if (m_CurrentTool != null && m_Input.IsInputEnabled)
                m_CurrentTool.Enable(this);

            Services.Events.Dispatch(Event_ToolSwitched, new ToolState(inTool, true));
            return true;
        }

        private bool SetToolState(ToolId inTool, bool state, bool inbForce)
        {
            var tool = GetTool(inTool);
            if (!inbForce && tool.IsEnabled() == state) {
                return false;
            }

            if (state)
                tool.Enable(this);
            else
                tool.Disable();

            if (inTool == ToolId.Flashlight) {
                Script.WriteVariable(Var_LastFlashlightState, state);
            }

            Services.Events.Dispatch(Event_ToolSwitched, new ToolState(inTool, state));
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
                case ToolId.Flashlight:
                    return m_Flashlight;
                case ToolId.Microscope:
                    return m_Microscope;
                case ToolId.NONE:
                    return NullTool.Instance;
                default:
                    Assert.Fail("unknown toolid {0}", inToolId);
                    return null;
            }
        }

        private void OnInventoryUpdated(StringHash32 inItemId)
        {
            UpdateUpgradeMask();

            if (m_CurrentToolId != ToolId.NONE)
                return;

            if (inItemId == ItemIds.ROVScanner)
                SwitchTool(ToolId.Scanner, false);
            else if (inItemId == ItemIds.ROVTagger)
                SwitchTool(ToolId.Tagger, false);
            else if (inItemId == ItemIds.Flashlight)
                SetToolState(ToolId.Flashlight, true, false);
        }

        private void UpdateUpgradeMask() {
            PassiveUpgradeMask upgrades = 0;
            if (Save.Inventory.HasUpgrade(ItemIds.Engine)) {
                upgrades |= PassiveUpgradeMask.Engine;
            }
            if (Save.Inventory.HasUpgrade(ItemIds.PropGuard)) {
                upgrades |= PassiveUpgradeMask.PropGuard;
            }
            m_UpgradeMask = upgrades;
        }

        // TODO: Implement
        // private IEnumerator StunRoutine()
        // {
            
        // }

        private void OnEnterTrigger(Collider2D other) {
            if (other.CompareTag(GameTags.WaterCurrent)) {
                m_EngineRegionCount++;
            } else if (other.CompareTag(GameTags.ThickVegetation)) {
                m_SlowRegionCount++;
            }
        }

        private void OnExitTrigger(Collider2D other) {
            if (other.CompareTag(GameTags.WaterCurrent)) {
                m_EngineRegionCount--;
            } else if (other.CompareTag(GameTags.ThickVegetation)) {
                m_SlowRegionCount--;
            }
        }

        public override void TeleportTo(Vector3 inPosition, FacingId inFacing = FacingId.Invalid)
        {
            base.TeleportTo(inPosition);

            m_Animator.HandleTeleport(inFacing);
        }

        #region Leaf

        [LeafMember("SetTool"), UnityEngine.Scripting.Preserve]
        private void LeafSetTool(ToolId inToolId)
        {
            SwitchTool(inToolId, false);
        }

        #endregion // Leaf
    }
}