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

        protected virtual void Awake()
        {
            this.CacheComponent(ref m_Transform);
        }

        public void TeleportTo(Transform inTransform)
        {
            TeleportTo(inTransform.position);
        }

        public void TeleportTo(SpawnLocation inSpawn)
        {
            TeleportTo(inSpawn.transform.position);
        }

        public virtual void TeleportTo(Vector3 inPosition)
        {
            m_Transform.position = inPosition;
        }
    }
}