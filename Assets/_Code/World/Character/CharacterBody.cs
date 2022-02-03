using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    public class CharacterBody : ScriptComponent
    {
        #region Inspector

        [SerializeField, Required] protected KinematicObject2D m_Kinematics = null;

        #endregion // Inspector

        [NonSerialized] protected Transform m_Transform;
        [NonSerialized] protected FacingId m_Facing;

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
    }
}