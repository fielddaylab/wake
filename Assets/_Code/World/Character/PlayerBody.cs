using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf.Runtime;
using UnityEngine;

namespace Aqua.Character
{
    public abstract class PlayerBody : CharacterBody
    {
        private readonly HashSet<StringHash32> m_CurrentRegionIds = new HashSet<StringHash32>();
        [NonSerialized] protected PlayerBodyStatus m_BodyStatus;

        public PlayerBodyStatus BodyStatus { get { return m_BodyStatus; } }

        private void FixedUpdate()
        {
            if (!Services.Physics.Enabled)
                return;

            Tick(Time.fixedDeltaTime);
        }

        protected abstract void Tick(float inDeltaTime);

        #region Region Tracking

        public void AddRegion(StringHash32 inId)
        {
            m_CurrentRegionIds.Add(inId);
        }

        public void RemoveRegion(StringHash32 inId)
        {
            m_CurrentRegionIds.Remove(inId);
        }

        public bool InRegion(StringHash32 inId)
        {
            return m_CurrentRegionIds.Contains(inId);
        }

        #endregion // Region Tracking

        #region Leaf

        [LeafMember("PlayerInRegion")]
        static private bool PlayerInRegion(StringHash32 inId)
        {
            return Script.CurrentPlayer?.InRegion(inId) ?? false;
        }

        #endregion // Leaf
    }

    [Flags]
    public enum PlayerBodyStatus : uint {
        Normal = 0,
        Stunned = 0x01,
        Slowed = 0x02,
        PowerEngineEngaged = 0x04,
    }
}