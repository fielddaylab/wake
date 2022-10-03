#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauData;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using EasyBugReporter;
using UnityEngine;

namespace Aqua.Profile
{
    public class BestiaryData : IProfileChunk, ISerializedVersion, ISerializedCallbacks
    {
        private const BFDiscoveredFlags TempDiscoveredMask = BFDiscoveredFlags.HasPair;

        #region Types

        private struct FactData : ISerializedObject, IKeyValuePair<StringHash32, FactData>
        {
            public StringHash32 FactId;
            public BFDiscoveredFlags Flags;

            public StringHash32 Key { get { return FactId; } }
            public FactData Value { get { return this; } }

            public void Serialize(Serializer ioSerializer)
            {
                ioSerializer.UInt32Proxy("id", ref FactId);
                ioSerializer.Enum("flags", ref Flags);
            }
        }

        [Flags]
        public enum TabFlags {
            Critters = 0x01,
            Environments = 0x02,
            Specters = 0x04
        }

        #endregion // Types

        private HashSet<StringHash32> m_ObservedEntities = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_ObservedFacts = new HashSet<StringHash32>();
        private RingBuffer<FactData> m_FactMetas = new RingBuffer<FactData>();
        private TabFlags m_UnlockedTabs = 0;

        [NonSerialized] private bool m_HasChanges = false;

        #region Observed Entities

        public bool HasEntity(StringHash32 inEntityId)
        {
            Assert.True(Services.Assets.Bestiary.HasId(inEntityId), "Entity with id '{0}' does not exist", inEntityId);
            return m_ObservedEntities.Contains(inEntityId);
        }

        public bool RegisterEntity(StringHash32 inEntityId)
        {
            Assert.True(Services.Assets.Bestiary.HasId(inEntityId), "Entity with id '{0}' does not exist", inEntityId);
            if (m_ObservedEntities.Add(inEntityId))
            {
                m_HasChanges = true;
                m_UnlockedTabs |= GetTab(inEntityId);
                Services.Events.Queue(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Entity, inEntityId));
                return true;
            }

            return false;
        }

        public IEnumerable<BestiaryDesc> GetEntities()
        {
            foreach(var entityId in m_ObservedEntities)
                yield return Assets.Bestiary(entityId);
        }

        public IEnumerable<BestiaryDesc> GetEntities(BestiaryDescCategory inCategory)
        {
            foreach(var entityId in m_ObservedEntities)
            {
                BestiaryDesc desc = Assets.Bestiary(entityId);
                if (desc.HasCategory(inCategory))
                    yield return desc;
            }
        }

        public int GetEntityCount(BestiaryDescCategory inCategory)
        {
            int count = 0;
            foreach(var entityId in m_ObservedEntities)
            {
                BestiaryDesc desc = Assets.Bestiary(entityId);
                if (desc.HasCategory(inCategory))
                    count++;
            }
            return count;
        }

        public int GetEntities(BestiaryDescCategory inCategory, ICollection<BestiaryDesc> outFacts)
        {
            int count = 0;
            foreach(var entityId in m_ObservedEntities)
            {
                BestiaryDesc desc = Assets.Bestiary(entityId);
                if (desc.HasCategory(inCategory))
                {
                    outFacts.Add(desc);
                    count++;
                }
            }
            return count;
        }

        public int GetEntities(BestiaryDescCategory inCategory, ICollection<TaggedBestiaryDesc> outEntities)
        {
            int count = 0;
            foreach(var entityId in m_ObservedEntities)
            {
                BestiaryDesc desc = Assets.Bestiary(entityId);
                if (desc.HasCategory(inCategory))
                {
                    outEntities.Add(new TaggedBestiaryDesc(desc));
                    count++;
                }
            }
            return count;
        }

        public bool DeregisterEntity(StringHash32 inEntityId)
        {
            Assert.True(Services.Assets.Bestiary.HasId(inEntityId), "Entity with id '{0}' does not exist", inEntityId);
            
            if (m_ObservedEntities.Remove(inEntityId))
            {
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.RemovedEntity, inEntityId));
                return true;
            }

            return false;
        }

        [MethodImpl(256)]
        static private TabFlags GetTab(StringHash32 inEntityId)
        {
            return GetTab(Assets.Bestiary(inEntityId));
        }

        static private TabFlags GetTab(BestiaryDesc inEntity)
        {
            if (inEntity.HasFlags(BestiaryDescFlags.IsSpecter)) {
                return TabFlags.Specters;
            } else if (inEntity.Category() == BestiaryDescCategory.Critter) {
                return TabFlags.Critters;
            } else {
                return TabFlags.Environments;
            }
        }

        #endregion // Observed Entities

        #region Facts

        public bool HasFact(StringHash32 inFactId)
        {
            Assert.True(Services.Assets.Bestiary.HasFactWithId(inFactId), "Fact with id '{0}' does not exist", inFactId);
            return m_ObservedFacts.Contains(inFactId) || Services.Assets.Bestiary.IsAutoFact(inFactId);
        }

        public bool RegisterFact(StringHash32 inFactId, bool inbIncludeEntity = false)
        {
            Assert.True(Services.Assets.Bestiary.HasFactWithId(inFactId), "Fact with id '{0}' does not exist", inFactId);

            if (Services.Assets.Bestiary.IsAutoFact(inFactId))
            {
                return false;
            }

            if (m_ObservedFacts.Add(inFactId))
            {
                m_HasChanges = true;
                var fact = Assets.Fact(inFactId);
                StringHash32 parentId = fact.Parent.Id();
                bool bVisible;
                if (inbIncludeEntity)
                {
                    m_ObservedEntities.Add(parentId);
                    m_UnlockedTabs |= GetTab(fact.Parent);
                    bVisible = true;
                }
                else
                {
                    bVisible = m_ObservedEntities.Contains(parentId);
                }
                if (bVisible)
                {
                    Services.Events.Queue(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Fact, inFactId));
                }
                return true;
            }

            return false;
        }

        public IEnumerable<BFBase> GetFactsForEntity(StringHash32 inEntityId)
        {
            BestiaryDesc entry = Assets.Bestiary(inEntityId);

            foreach(var fact in entry.Facts)
            {
                switch(fact.Mode)
                {
                    case BFMode.Player:
                        if (m_ObservedFacts.Contains(fact.Id))
                            yield return fact;
                        break;

                    case BFMode.Always:
                        yield return fact;
                        break;
                }
            }
        }

        public int GetFactsForEntity(StringHash32 inEntityId, ICollection<BFBase> outFacts)
        {
            BestiaryDesc entry = Assets.Bestiary(inEntityId);
            int count = 0;

            foreach(var fact in entry.Facts)
            {
                switch(fact.Mode)
                {
                    case BFMode.Player:
                        if (m_ObservedFacts.Contains(fact.Id))
                        {
                            outFacts.Add(fact);
                            count++;
                        }
                        break;

                    case BFMode.Always:
                        outFacts.Add(fact);
                        count++;
                        break;
                }
            }

            return count;
        }

        public bool DeregisterFact(StringHash32 inFactId)
        {
            Assert.True(Services.Assets.Bestiary.HasFactWithId(inFactId), "Fact with id '{0}' does not exist", inFactId);

            if (m_ObservedFacts.Remove(inFactId))
            {
                int metaIdx = m_FactMetas.BinarySearch(inFactId);
                if (metaIdx >= 0)
                {
                    m_FactMetas.FastRemoveAt(metaIdx);
                    m_FactMetas.SortByKey<StringHash32, FactData>();
                }
                
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.RemovedFact, inFactId));
                return true;
            }

            return false;
        }

        public BFDiscoveredFlags GetDiscoveredFlags(StringHash32 inFactId)
        {
            if (!HasFact(inFactId))
                return BFDiscoveredFlags.None;

            BFBase fact = Assets.Fact(inFactId);
            BFDiscoveredFlags flags = BFType.DefaultDiscoveredFlags(fact);
            StringHash32 pair = BFType.PairId(fact);
            int metaIdx = m_FactMetas.BinarySearch(inFactId);
            if (metaIdx >= 0)
                flags |= m_FactMetas[metaIdx].Flags;
            if (!pair.IsEmpty && (m_ObservedFacts.Contains(pair) || Services.Assets.Bestiary.IsAutoFact(pair)))
                flags |= BFDiscoveredFlags.HasPair;
            if (fact.Parent.HasFlags(BestiaryDescFlags.IsSpecter) && !Save.Science.FullyDecrypted())
                flags |= BFDiscoveredFlags.IsEncrypted;
            return flags;
        }

        public bool AddDiscoveredFlags(StringHash32 inFactId, BFDiscoveredFlags inFlags)
        {
            inFlags &= ~TempDiscoveredMask;

            if (inFlags <= 0)
                return false;
            
            RegisterFact(inFactId);
            BFBase fact = Assets.Fact(inFactId);

            BFDiscoveredFlags existingFlags = BFType.DefaultDiscoveredFlags(fact);
            int metaIdx = m_FactMetas.BinarySearch(inFactId);
            if (metaIdx >= 0)
                existingFlags |= m_FactMetas[metaIdx].Flags;
            if ((existingFlags & inFlags) == inFlags)
                return false;

            if (metaIdx >= 0)
            {
                m_FactMetas[metaIdx].Flags |= inFlags;
            }
            else
            {
                FactData data;
                data.FactId = inFactId;
                data.Flags = inFlags;
                m_FactMetas.PushBack(data);
                m_FactMetas.SortByKey<StringHash32, FactData>();
            }

            bool bVisible = m_ObservedEntities.Contains(fact.Parent.Id());
            if (bVisible)
            {
                Services.Events.Queue(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.UpgradeFact, inFactId));
            }

            m_HasChanges = true;
            return true;
        }

        public bool RemoveDiscoveredFlags(StringHash32 inFactId, BFDiscoveredFlags inFlags)
        {
            inFlags &= ~TempDiscoveredMask;

            if (inFlags <= 0)
                return false;
            
            BFBase fact = Assets.Fact(inFactId);

            BFDiscoveredFlags existingFlags = BFType.DefaultDiscoveredFlags(fact);
            if ((existingFlags & inFlags) == inFlags)
            {
                return false;
            }

            int metaIdx = m_FactMetas.BinarySearch(inFactId);
            if (metaIdx >= 0)
            {
                if ((m_FactMetas[metaIdx].Flags & inFlags) == 0)
                {
                    return false;
                }

                m_FactMetas[metaIdx].Flags &= ~inFlags;
                bool bVisible = m_ObservedEntities.Contains(fact.Parent.Id());
                if (bVisible)
                {
                    Services.Events.Queue(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.UpgradeFact, inFactId));
                }

                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool IsFactFullyUpgraded(StringHash32 inFactId)
        {
            return (GetDiscoveredFlags(inFactId) & BFDiscoveredFlags.All) == BFDiscoveredFlags.All;
        }

        #endregion // Facts

        public bool HasTab(TabFlags tab) {
            return (m_UnlockedTabs & tab) != 0;
        }

        #region IProfileChunk

        // v3: added metas
        // v4: removed graphed
        // v5: added tabs
        ushort ISerializedVersion.Version { get { return 4; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32ProxySet("allEntities", ref m_ObservedEntities);
            ioSerializer.UInt32ProxySet("allFacts", ref m_ObservedFacts);
            if (ioSerializer.ObjectVersion < 4)
            {
                int[] arr = null;
                ioSerializer.Array("graphedFacts", ref arr);
            }
            if (ioSerializer.ObjectVersion >= 3)
            {
                ioSerializer.ObjectArray("factMetas", ref m_FactMetas);
            }
            if (ioSerializer.ObjectVersion >= 5)
            {
                ioSerializer.Enum("unlockedTabs", ref m_UnlockedTabs);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            #endif // UNITY_EDITOR

            var bestiary = Services.Assets.Bestiary;

            SavePatcher.PatchIds(m_ObservedEntities);
            SavePatcher.PatchIds(m_ObservedFacts);

            for(int i = 0; i < m_FactMetas.Count; i++) {
                ref var meta = ref m_FactMetas[i];
                SavePatcher.PatchId(ref meta.FactId);
            }

            m_ObservedEntities.RemoveWhere((entityId) => {
                if (!bestiary.HasId(entityId))
                {
                    Log.Warn("[BestiaryData] Unknown entity id '{0}'", entityId);
                    return true;
                }

                return false;
            });

            m_ObservedFacts.RemoveWhere((factId) => {
                if (!bestiary.HasFactWithId(factId))
                {
                    Log.Warn("[BestiaryData] Unknown fact id '{0}'", factId);
                    return true;
                }

                return false;
            });

            if (m_UnlockedTabs == 0 && m_ObservedEntities.Count > 0) {
                foreach(var entId in m_ObservedEntities) {
                    m_UnlockedTabs |= GetTab(entId);
                }
            }
        }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        public void Dump(EasyBugReporter.IDumpWriter writer) {
            writer.Header("Bestiary Entities");
            foreach(var entityId in m_ObservedEntities) {
                writer.Text(Assets.NameOf(entityId));
            }
            
            writer.Header("Discovered Facts");
            foreach(var factId in m_ObservedFacts) {
                writer.KeyValue(Assets.NameOf(factId), GetDiscoveredFlags(factId));
            }
        }

        #endregion // IProfileChunk

        #region Debug

        #if DEVELOPMENT

        internal bool DebugRegisterEntityNoEvent(StringHash32 inEntityId)
        {
            Assert.True(Services.Assets.Bestiary.HasId(inEntityId), "Entity with id '{0}' does not exist", inEntityId);
            if (m_ObservedEntities.Add(inEntityId))
            {
                m_UnlockedTabs |= GetTab(inEntityId);
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        internal bool DebugRegisterFactNoEvent(StringHash32 inFactId, bool inbIncludeEntity = false)
        {
            if (Services.Assets.Bestiary.IsAutoFact(inFactId))
            {
                return false;
            }

            if (m_ObservedFacts.Add(inFactId))
            {
                m_HasChanges = true;
                var fact = Assets.Fact(inFactId);
                StringHash32 parentId = fact.Parent.Id();
                if (inbIncludeEntity)
                {
                    m_ObservedEntities.Add(parentId);
                    m_UnlockedTabs |= GetTab(fact.Parent);
                }
                return true;
            }

            return false;
        }

        public bool DebugRegisterFactFlagsNoEvent(StringHash32 inFactId, BFDiscoveredFlags inFlags)
        {
            if (inFlags <= 0)
                return false;
            
            DebugRegisterFactNoEvent(inFactId);
            BFBase fact = Assets.Fact(inFactId);

            BFDiscoveredFlags existingFlags = BFType.DefaultDiscoveredFlags(fact);
            int metaIdx = m_FactMetas.BinarySearch(inFactId);
            if (metaIdx > 0)
                existingFlags |= m_FactMetas[metaIdx].Flags;
            if ((existingFlags & inFlags) == inFlags)
                return false;

            if (metaIdx > 0)
            {
                m_FactMetas[metaIdx].Flags |= inFlags;
            }
            else
            {
                FactData data;
                data.FactId = inFactId;
                data.Flags = inFlags;
                m_FactMetas.PushBack(data);
                m_FactMetas.SortByKey<StringHash32, FactData>();
            }

            m_HasChanges = true;
            return true;
        }

        #endif // DEVELOPMENT

        #endregion // Debug
    }
}