using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using Aqua;
using Aqua.Character;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua.StationMap
{    
    public class PlayerController : PlayerBody
    {
        #region Inspector

        [SerializeField] private PlayerInput m_Input = null;
        [SerializeField] private PlayerAnimator m_Animator = null;
        
        [Header("Movement Params")]

        [SerializeField] private MovementFromOffsetAndHeading m_MoveParams = default;
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineOff = 2;

        #endregion // Inspector

        protected override void Awake() {
            base.Awake();

            Services.Events.Register(GameEvents.ProfileSaveBegin, WriteCustomSpawn, this)
                .Register(GameEvents.SceneWillUnload, WriteCustomSpawn, this);
        }

        protected void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        public override void PrepareSpawn() {
            Spawner.CustomSpawn = HandleCustomSpawn;
        }

        protected override void Tick(float inDeltaTime)
        {
            PlayerInput.Input input;
            m_Input.GenerateInput(out input);

            if (input.Move && input.MovementVector.sqrMagnitude > 0)
            {
                m_MoveParams.Apply(input.MovementVector, m_Kinematics, inDeltaTime);

                m_Kinematics.Config.Drag = m_DragEngineOn;
            }
            else
            {
                m_Kinematics.Config.Drag = m_DragEngineOff;
            }
        }

        public override void TeleportTo(Vector3 inPosition, FacingId inFacing = FacingId.Invalid)
        {
            base.TeleportTo(inPosition);

            if (inFacing != FacingId.Invalid)
            {
                Vector2 look = Facing.Look(inFacing);
                float angle = Mathf.Atan2(look.y, look.x);
                m_Transform.SetRotation(angle * Mathf.Rad2Deg, Axis.Z, Space.Self);
            }

            m_Animator.OnTeleport();
        }

        #region Custom Spawn

        static private readonly TableKeyPair Var_StationMapCoordsId = TableKeyPair.Parse("world:stationMapCoords.stationId");
        static private readonly TableKeyPair Var_StationMapCoordsX = TableKeyPair.Parse("world:stationMapCoords.x");
        static private readonly TableKeyPair Var_StationMapCoordsY = TableKeyPair.Parse("world:stationMapCoords.y");
        static private readonly TableKeyPair Var_StationMapCoordsFacing = TableKeyPair.Parse("world:stationMapCoords.facing");

        static private SpawnCtrl.CustomSpawnHandler HandleCustomSpawn = (p, e) => {
            if (!e.IsEmpty && e != MapIds.Helm) {
                return false;
            }

            StringHash32 mapId = MapDB.LookupCurrentMap();
            StringHash32 lastMapId = Script.ReadVariable(Var_StationMapCoordsId).AsStringHash();
            Vector3 coords = p.transform.position;
            if (mapId != lastMapId) {
                Script.WriteVariable(Var_StationMapCoordsId, mapId);
                Script.WriteVariable(Var_StationMapCoordsX, coords.x);
                Script.WriteVariable(Var_StationMapCoordsY, coords.y);
                Script.WriteVariable(Var_StationMapCoordsFacing, (int) p.FaceDirection);
            } else {
                coords.x = Script.ReadVariable(Var_StationMapCoordsX, coords.x).AsFloat();
                coords.y = Script.ReadVariable(Var_StationMapCoordsY, coords.y).AsFloat();
                FacingId facing = (FacingId) Script.ReadVariable(Var_StationMapCoordsFacing, (int) FacingId.Invalid).AsInt();
                p.TeleportTo(coords, facing);
            }

            return true;
        };

        static private void WriteCustomSpawn() {
            PlayerBody body = Services.State.Player;
            StringHash32 mapId = MapDB.LookupCurrentMap();
            Vector3 coords = body.transform.position;
            FacingId facing = Facing.FromVector(Geom.Normalized(body.transform.localEulerAngles.z * Mathf.Deg2Rad));
            Script.WriteVariable(Var_StationMapCoordsId, mapId);
            Script.WriteVariable(Var_StationMapCoordsX, coords.x);
            Script.WriteVariable(Var_StationMapCoordsY, coords.y);
            Script.WriteVariable(Var_StationMapCoordsFacing, (int) facing);
        }

        #endregion // Custom Spawn
    }
}

