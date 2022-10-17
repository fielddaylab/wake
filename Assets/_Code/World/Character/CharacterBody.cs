using System;
using Aqua.Scripting;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua.Character
{
    public class CharacterBody : ScriptComponent
    {
        #region Inspector

        [SerializeField, Required] protected KinematicObject2D m_Kinematics = null;

        #endregion // Inspector

        [NonSerialized] protected Transform m_Transform;
        [NonSerialized] protected FacingId m_Facing;

        public KinematicObject2D Kinematics { get { return m_Kinematics; } }
        public FacingId FaceDirection { get { return m_Facing; } }

        protected virtual void Awake()
        {
            this.CacheComponent(ref m_Transform);
        }

        public void TeleportTo(Transform inTransform, FacingId inFacing = FacingId.Invalid)
        {
            TeleportTo(inTransform.position, inFacing);
        }

        public void TeleportTo(SpawnLocation inSpawn)
        {
            TeleportTo(inSpawn.Location.position, inSpawn.Facing);
        }

        public virtual void TeleportTo(Vector3 inPosition, FacingId inFacing = FacingId.Invalid)
        {
            m_Transform.position = inPosition;
        }

        [LeafMember("TeleportTo"), Preserve]
        private void LeafTeleportTo(StringHash32 inObjectId)
        {
            Services.Script.TryGetScriptObjectById(inObjectId, out ScriptObject obj);
            var location = obj.GetComponent<SpawnLocation>();
            if (location != null) {
                TeleportTo(location);
            } else {
                TeleportTo(obj.transform);
            }
        }
    }
}