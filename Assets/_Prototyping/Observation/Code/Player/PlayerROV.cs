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
using UnityEngine.Scripting;

namespace ProtoAqua.Observation
{
    public class PlayerROV : PlayerBody, ISceneLoadHandler
    {
        static public readonly TableKeyPair Var_LastFlashlightState = TableKeyPair.Parse("player:lastFlashlightState");

        static public readonly StringHash32 Event_RequestToolToggle = "PlayerROV::RequestToolToggle"; // ToolState
        static public readonly StringHash32 Event_RequestToolSwitch = "PlayerROV::RequestToolSwitch"; // tool id
        static public readonly StringHash32 Event_ToolSwitched = "PlayerROV::ToolSwitched"; // ToolState
        static public readonly StringHash32 Event_ToolPermissions = "PlayerROV::ToolPermissions"; // ToolState

        static public readonly StringHash32 Trigger_ToolActivated = "ToolActivated";
        static public readonly StringHash32 Trigger_ToolDeactivated = "ToolDeactivated";

        static public readonly StringHash32 Trigger_Dashed = "ROVDashed";
        static public readonly StringHash32 Trigger_DashCollision = "ROVDashCollision";
        static public readonly StringHash32 Trigger_HardCollision = "ROVHardCollision";

        #region Types

        static private readonly string[] ToolIdToString = Enum.GetNames(typeof(ToolId));

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
            Breaker,

            NONE
        }

        [Flags]
        public enum PassiveUpgrades {
            Engine = 0x01,
            PropGuard = 0x02,
            Hull = 0x04
        }

        static public bool ToolIsPassive(ToolId id) {
            switch(id) {
                case ToolId.Flashlight:
                case ToolId.Microscope:
                    return true;

                default:
                    return false;
            }
        }

        static public StringHash32 ToolIdToItemId(ToolId id) {
            switch(id) {
                case ToolId.Scanner:
                    return ItemIds.ROVScanner;
                case ToolId.Tagger:
                    return ItemIds.ROVTagger;
                case ToolId.Breaker:
                    return ItemIds.Icebreaker;
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
            bool UpdateTool(float inDeltaTime, in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody);
            void UpdateActive(float inDeltaTime, in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody);
            bool HasTarget();
            PlayerROVAnimationFlags AnimFlags();
            float MoveSpeedMultiplier();
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
            public PlayerROVAnimationFlags AnimFlags() { return 0; }
            public float MoveSpeedMultiplier() { return 1; }
            public bool UpdateTool(float inDeltaTime, in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) { return false; }
            public void UpdateActive(float inDeltaTime, in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) { }
        }

        #endregion // Types

        #region Inspector

        [SerializeField, Required] private PlayerROVWorldUI m_WorldUI = null;
        [SerializeField, Required] private PlayerROVInput m_Input = null;
        [SerializeField, Required] private PlayerROVScanner m_Scanner = null;
        [SerializeField, Required] private PlayerROVTagger m_Tagger = null;
        [SerializeField, Required] private PlayerROVBreaker m_Breaker = null;
        [SerializeField, Required] private PlayerROVFlashlight m_Flashlight = null;
        [SerializeField, Required] private PlayerROVMicroscope m_Microscope = null;
        [SerializeField, Required] private PlayerROVAnimator m_Animator = null;
        [SerializeField, Required] private Collider2D m_TriggerCollider = null;

        [Header("Movement Params")]

        [SerializeField] private MovementFromOffset m_MovementParams = default(MovementFromOffset);
        [SerializeField] private MovementFromOffset m_MovementPowerParams = default(MovementFromOffset);
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineDash = 1;
        [SerializeField] private float m_DragEngineOff = 2;
        [SerializeField] private float m_DashSpeed = 15;
        [SerializeField] private float m_DashDuration = 0.5f;

        [Header("Camera Params")]

        [SerializeField] private float m_CameraForwardLook = 1;
        [SerializeField] private float m_CameraForwardLookWeight = 0.5f;
        [SerializeField] private float m_CameraForwardLookNoMove = 1;
        [SerializeField] private float m_CameraZoomTool = 1.1f;

        [Header("Additional Settings")]
        [SerializeField] private bool m_DreamMode = false;
        
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
        [NonSerialized] private PassiveUpgrades m_UpgradeMask = 0;
        [NonSerialized] private Routine m_StunRoutine;

        [NonSerialized] private int m_EngineRegionCount;
        [NonSerialized] private int m_SlowRegionCount;
        [NonSerialized] private float m_MouseLookScale = 1;
        [NonSerialized] private float m_DashLeft = 0;
        [NonSerialized] private Vector2 m_InitialDashVector;

        [NonSerialized] private CollisionResponseSystem m_CollisionResponder;

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

            m_Kinematics.OnContact.Register(OnContacts);

            m_CurrentTool = NullTool.Instance;

            WorldUtils.ListenForLayerMask(m_TriggerCollider, GameLayers.PlayerTrigger_Mask, OnEnterTrigger, OnExitTrigger);
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

            PlayerBodyStatus status = m_BodyStatus & ~PlayerBodyStatus.TempMask;
            if (m_StunRoutine) {
                status |= PlayerBodyStatus.Stunned;
            }
            if (m_EngineRegionCount > 0) {
                if ((m_UpgradeMask & PassiveUpgrades.Engine) != 0) {
                    status |= PlayerBodyStatus.PowerEngineEngaged;
                } else {
                    status |= PlayerBodyStatus.DraggedByCurrent;
                }
            }
            if (m_SlowRegionCount > 0) {
                status |= PlayerBodyStatus.Slowed;
            }

            if (m_DashLeft > 0) {
                m_DashLeft -= inDeltaTime;
                status |= PlayerBodyStatus.PowerEngineEngaged | PlayerBodyStatus.Dashing;
            }

            m_BodyStatus = status;

            m_Input.GenerateInput(m_Transform, lockOn, status, inDeltaTime, m_LastInputData, out m_LastInputData);

            if (m_Moving || !UpdateTool(inDeltaTime))
            {
                UpdateMove(inDeltaTime);
            }

            UpdateKinematicConfig();
            m_CurrentTool.UpdateActive(inDeltaTime, m_LastInputData, m_Kinematics.State.Velocity, this);
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
                if ((BodyStatus & PlayerBodyStatus.SilentMovement) != 0) {
                    m_EngineSound.Stop(0.25f);
                } else {
                    m_EngineSound.SetPitch(Mathf.Clamp01(m_Kinematics.State.Velocity.magnitude / m_Kinematics.Config.MaxSpeed));
                }
            }

            velocityHintData.WeightOffset = (m_Moving ? 1f : 0.2f) * m_CameraForwardLookWeight;
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
                mouseHintData.WeightOffset = m_CameraForwardLookWeight * m_MouseLookScale;
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
            animState.AnimFlags = m_CurrentTool.AnimFlags();
            animState.Moving = m_Moving;
            animState.UsingTool = m_CurrentTool.HasTarget();
            animState.NormalizedLook = m_LastInputData.Mouse.NormalizedOffset;
            m_CurrentTool.GetTargetPosition(false, out animState.LookTarget, out var _);
            animState.NormalizedMove = m_LastInputData.MoveVector;
            if (m_DashLeft > 0) {
                animState.NormalizedMove = m_InitialDashVector;
            }
            animState.Status = m_BodyStatus;
            m_Animator.Process(animState);
        }

        #region Movement

        private void UpdateMove(float inDeltaTime)
        {
            if (m_LastInputData.Dash > 0 && (m_UpgradeMask & PassiveUpgrades.Engine) != 0 && !(m_Microscope && m_Microscope.IsEnabled()))
            {
                m_InitialDashVector = m_LastInputData.MoveVector.normalized;
                m_Kinematics.State.Velocity += m_InitialDashVector * m_DashSpeed;
                
                if ((BodyStatus & PlayerBodyStatus.SilentMovement) == 0) {
                    if (m_LastInputData.Dash == PlayerROVInput.DashType.Primary) {
                        Services.Audio.PostEvent("ROV.Engine.Dash");
                        Services.Camera.AddShake(0.04f, 0.15f, 0.3f);
                    } else {
                        Services.Audio.PostEvent("ROV.Engine.SecondaryDash");
                        Services.Camera.AddShake(0.02f, 0.15f, 0.3f);
                    }
                }
                m_DashLeft = m_DashDuration;
                Services.Script.QueueTriggerResponse(Trigger_Dashed, -10000);
                Log.Msg("[PlayerROV] Dash activated");
                SetEngineState(true);
                return;
            }

            if (m_DashLeft > 0)
            {
                SetEngineState(true);
                return;
            }
            
            if (m_LastInputData.Move)
            {
                float dist = m_LastInputData.MoveVector.magnitude;
                float moveMultiplier = m_CurrentTool.MoveSpeedMultiplier();
                if (m_Microscope && m_Microscope.IsEnabled()) {
                    moveMultiplier *= m_Microscope.MoveSpeedMultiplier();
                }
                
                SetEngineState(dist > 0);
                if (dist > 0)
                {
                    GetMoveParams().Apply(m_LastInputData.MoveVector, m_Kinematics, inDeltaTime, moveMultiplier);
                }
                return;
            }
            
            SetEngineState(false);
        }

        private MovementFromOffset GetMoveParams() {
            if ((m_UpgradeMask & PassiveUpgrades.Engine) != 0) {
                return m_MovementPowerParams;
            } else {
                return m_MovementParams;
            }
        }

        private void UpdateKinematicConfig() {
            if (m_Moving) {
                m_Kinematics.enabled = true;
                m_Kinematics.Config.Drag = m_DashLeft > 0 ? m_DragEngineDash : m_DragEngineOn;
            } else {
                m_Kinematics.Config.Drag = m_DragEngineOff;
                m_Kinematics.enabled = m_Input.IsInputEnabled;
            }
        }

        private void SetEngineState(bool inbOn, bool inbForce = false)
        {
            if (!inbForce && m_Moving == inbOn)
                return;

            m_Moving = inbOn;
            if (inbOn)
            {
                if ((BodyStatus & PlayerBodyStatus.SilentMovement) == 0) {
                    m_EngineSound = Services.Audio.PostEvent("rov_engine_loop");
                    m_EngineSound.SetVolume(0).SetVolume(1, 0.25f);
                }
            }
            else
            {
                m_EngineSound.Stop(0.25f);
            }
        }
    
        #endregion // Movement

        #region Collisions

        private unsafe void OnContacts() {
            int contactCount = m_Kinematics.ContactCount;
            PhysicsContact* contacts = m_Kinematics.Contacts;
            PhysicsContact contact;
            ColliderMaterialId materialId;
            float z = m_Transform.position.z;

            bool impacted = false;
            bool slid = false;
            for(int i = 0; i < contactCount; i++) {
                contact = contacts[i];
                materialId = ColliderMaterial.Find(contact.Collider.Cast<Collider2D>());
                if (contact.Impact > 4 && !impacted) {
                    Log.Msg("[PlayerROV] Impacted wall, reducing velocity");
                    impacted = true;
                    if (contact.Impact > 8) {
                        m_Kinematics.State.Velocity *= 0.25f;
                        m_DashLeft *= 0.25f;
                        m_Input.ClearDash();
                        if (materialId != ColliderMaterialId.Invisible) {
                            Services.Camera.AddShake(contact.State.Velocity.normalized * 0.15f, new Vector2(0.15f, 0.15f), 0.5f);
                            m_CollisionResponder?.Queue(materialId, CollisionResponseSystem.ImpactType.Heavy, PhysicsContact.OnPlane(contact.Point, z), contact.Normal, 1);
                            // TODO: Play impact noise

                            if (m_DashDuration > 0) {
                                Services.Script.QueueTriggerResponse(Trigger_DashCollision, -10000);
                            } else {
                                Services.Script.QueueTriggerResponse(Trigger_HardCollision, -10000);
                            }
                        } else {
                            Services.Camera.AddShake(contact.State.Velocity.normalized * 0.06f, new Vector2(0.15f, 0.15f), 0.4f);
                        }
                    } else {
                        m_Kinematics.State.Velocity *= 0.5f;
                        m_DashLeft *= 0.5f;
                        if (materialId != ColliderMaterialId.Invisible) {
                            m_CollisionResponder?.Queue(materialId, CollisionResponseSystem.ImpactType.Light, PhysicsContact.OnPlane(contact.Point, z), contact.Normal, 1);
                            // TODO: Play impact noise
                        }
                    }
                } else if (contact.Impact > 0.5f && contact.Impact < 3 && !slid) {
                    slid = true;
                    Log.Msg("[PlayerROV] Sliding on wall, reducing velocity");
                    m_Kinematics.State.Velocity *= 0.7f;
                }
            }
        }

        #endregion // Collisions

        #region Tools

        private bool UpdateTool(float inDeltaTime)
        {
            if (m_CurrentTool.UpdateTool(inDeltaTime, m_LastInputData, m_Kinematics.State.Velocity, this))
            {
                SetEngineState(false);
                return true;
            }

            return false;
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

        private bool SwitchTool(ToolId inTool, bool inbForce)
        {
            if (!inbForce && m_CurrentToolId == inTool)
                return false;

            if (m_CurrentTool != null && m_Input.IsInputEnabled) {
                m_CurrentTool.Disable();
                
                var tempTable = TempVarTable.Alloc();
                tempTable.Set("toolId", ToolIdToString[(int) m_CurrentToolId]);
                Services.Script.QueueTriggerResponse(Trigger_ToolDeactivated, 0, tempTable);
            }

            m_CurrentToolId = inTool;
            m_CurrentTool = GetTool(inTool);

            if (m_CurrentTool != null && m_Input.IsInputEnabled) {
                m_CurrentTool.Enable(this);

                var tempTable = TempVarTable.Alloc();
                tempTable.Set("toolId", ToolIdToString[(int) m_CurrentToolId]);
                Services.Script.QueueTriggerResponse(Trigger_ToolActivated, 0, tempTable);
            }

            Services.Events.Dispatch(Event_ToolSwitched, new ToolState(inTool, true));
            return true;
        }

        private bool SetToolState(ToolId inTool, bool state, bool inbForce)
        {
            var tool = GetTool(inTool);
            if (!(UnityEngine.Object)tool) {
                return false;
            }

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
            
            var tempTable = TempVarTable.Alloc();
            tempTable.Set("toolId", ToolIdToString[(int) inTool]);
            if (state) {
                Services.Script.QueueTriggerResponse(Trigger_ToolActivated, -500, tempTable);
            } else {
                Services.Script.QueueTriggerResponse(Trigger_ToolDeactivated, -500, tempTable);
            }

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
                case ToolId.Breaker:
                    return m_Breaker;
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
            ApplyPassiveUpgrades();

            if (inItemId == ItemIds.Flashlight)
                SetToolState(ToolId.Flashlight, true, false);

            if (m_CurrentToolId != ToolId.NONE)
                return;

            if (inItemId == ItemIds.ROVScanner)
                SwitchTool(ToolId.Scanner, false);
            else if (inItemId == ItemIds.ROVTagger)
                SwitchTool(ToolId.Tagger, false);
            else if (inItemId == ItemIds.Icebreaker)
                SwitchTool(ToolId.Breaker, false);
        }

        private void UpdateUpgradeMask() {
            PassiveUpgrades upgrades = 0;
            if (!m_DreamMode) {
                if (Save.Inventory.HasUpgrade(ItemIds.Engine)) {
                    upgrades |= PassiveUpgrades.Engine;
                }
                if (Save.Inventory.HasUpgrade(ItemIds.PropGuard)) {
                    upgrades |= PassiveUpgrades.PropGuard;
                }
                if (Save.Inventory.HasUpgrade(ItemIds.Hull)) {
                    upgrades |= PassiveUpgrades.Hull;
                }
            }
            m_UpgradeMask = upgrades;
        }

        private void ApplyPassiveUpgrades() {
            if ((m_UpgradeMask & PassiveUpgrades.Engine) != 0) {
                m_Kinematics.ScaledForceMultiplier = 0.1f;
            } else {
                m_Kinematics.ScaledForceMultiplier = 1;
            }

            m_Animator.ApplyUpgradeMask(m_UpgradeMask);
        }

        #endregion // Tools

        #region Callbacks

        private void OnInputEnabled()
        {
            m_CurrentTool.Enable(this);
        }

        private void OnInputDisabled()
        {
            m_CurrentTool.Disable();
            m_DashLeft = 0;
        }

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

        #endregion // Callbacks

        public override void TeleportTo(Vector3 inPosition, FacingId inFacing = FacingId.Invalid)
        {
            base.TeleportTo(inPosition);

            m_Animator.HandleTeleport(inFacing);
        }

        #region ISceneLoadHandler

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            m_CollisionResponder = Services.State.FindManager<CollisionResponseSystem>();

            ToolId lastToolId = ToolId.NONE;
            if (Save.Inventory.HasUpgrade(ItemIds.ROVScanner)) {
                lastToolId = ToolId.Scanner;
            }

            UpdateUpgradeMask();

            ApplyPassiveUpgrades();
            SwitchTool(lastToolId, true);

            SetToolState(ToolId.Flashlight, Script.ReadVariable(Var_LastFlashlightState).AsBool(), true);
            SetToolState(ToolId.Microscope, false, true);

            if (m_Input.IsInputEnabled)
                OnInputEnabled();
        }

        #endregion // ISceneLoadHandler

        #region Leaf

        [LeafMember("SetTool"), UnityEngine.Scripting.Preserve]
        private void LeafSetTool(ToolId inToolId)
        {
            SwitchTool(inToolId, false);
        }

        [LeafMember("ToggleToolOn"), UnityEngine.Scripting.Preserve]
        private void LeafToggleToolOn(ToolId inToolId)
        {
            SetToolState(inToolId, true, false);
        }

        [LeafMember("IsToolActive"), Preserve]
        static private bool LeafIsToolActive(ToolId inToolId)
        {
            PlayerROV rov = Services.State.Player as PlayerROV;
            if (rov == null)
                return false;

            return rov.GetTool(inToolId).IsEnabled();
        }

        [LeafMember("SetMouseLookScale"), Preserve]
        public void SetMouseLookScale(float scale) {
            m_MouseLookScale = scale;
        }

        [LeafMember("SetToolAllowed"), Preserve]
        static private void LeafSetToolAllowed(ToolId inToolId, bool allowed) {
            PlayerROV rov = Services.State.Player as PlayerROV;
            if (rov == null) {
                return;
            }

            var tool = rov.GetTool(inToolId);
            if (!allowed && tool.IsEnabled()) {
                tool.Disable();
                if (!ToolIsPassive(inToolId)) {
                    rov.SwitchTool(ToolId.Scanner, false);
                }
            }

            Services.Events.Dispatch(Event_ToolPermissions, new ToolState(inToolId, allowed));
        }

        #endregion // Leaf
    }
}