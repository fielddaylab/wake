using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua.Character
{
    public abstract class PlayerBody : CharacterBody, IBaked
    {
        private readonly HashSet<StringHash32> m_CurrentRegionIds = Collections.NewSet<StringHash32>(3);
        [NonSerialized] protected PlayerBodyStatus m_BodyStatus;
        [SerializeField, HideInInspector] private SpawnCtrl m_SpawnCtrl;

        public PlayerBodyStatus BodyStatus { get { return m_BodyStatus; } }
        public SpawnCtrl Spawner { get { return m_SpawnCtrl; } }

        private void FixedUpdate()
        {
            if (!Services.Physics.Enabled)
                return;

            Tick(Time.fixedDeltaTime);
        }

        public virtual void PrepareSpawn() { }

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

        [LeafMember("PlayerInRegion"), Preserve]
        static private bool PlayerInRegion(StringHash32 inId)
        {
            return Script.CurrentPlayer?.InRegion(inId) ?? false;
        }

        #endregion // Leaf

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order { get; }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            return BakeImpl(flags);
        }

        protected virtual bool BakeImpl(BakeFlags flags)
        {
            return Ref.Replace(ref m_SpawnCtrl, FindObjectOfType<SpawnCtrl>());
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }

    [Flags]
    public enum PlayerBodyStatus : uint {
        Normal = 0,
        Stunned = 0x01,
        Slowed = 0x02,
        PowerEngineEngaged = 0x04,
        DraggedByCurrent = 0x08,
        DisableMovement = 0x10,
        DisableTools = 0x20
    }
}