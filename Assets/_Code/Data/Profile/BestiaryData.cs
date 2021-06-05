using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Profile
{
    public class BestiaryData : IProfileChunk, ISerializedVersion
    {
        private HashSet<StringHash32> m_ObservedEntities = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_ObservedFacts = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_GraphedFacts = new HashSet<StringHash32>();

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
                Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Entity, inEntityId));
                return true;
            }

            return false;
        }

        public IEnumerable<BestiaryDesc> GetEntities()
        {
            foreach(var entity in m_ObservedEntities)
                yield return Services.Assets.Bestiary.Get(entity);
        }

        public IEnumerable<BestiaryDesc> GetEntities(BestiaryDescCategory inCategory)
        {
            foreach(var entity in m_ObservedEntities)
            {
                BestiaryDesc desc = Services.Assets.Bestiary.Get(entity);
                if (desc.HasCategory(inCategory))
                    yield return desc;
            }
        }

        public int GetEntities(BestiaryDescCategory inCategory, ICollection<BestiaryDesc> outFacts)
        {
            var db = Services.Assets.Bestiary;
            int count = 0;
            foreach(var entity in m_ObservedEntities)
            {
                BestiaryDesc desc = db.Get(entity);
                if (desc.HasCategory(inCategory))
                {
                    outFacts.Add(desc);
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
                Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.RemovedEntity, inEntityId));
                return true;
            }

            return false;
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
                var fact = Services.Assets.Bestiary.Fact(inFactId);
                StringHash32 parentId = fact.Parent().Id();
                bool bVisible;
                if (inbIncludeEntity)
                {
                    m_ObservedEntities.Add(parentId);
                    bVisible = true;
                }
                else
                {
                    bVisible = m_ObservedEntities.Contains(parentId);
                }
                if (bVisible)
                {
                    Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Fact, inFactId));
                }
                return true;
            }

            return false;
        }

        public IEnumerable<BFBase> GetFactsForEntity(StringHash32 inEntityId)
        {
            BestiaryDesc entry = Services.Assets.Bestiary.Get(inEntityId);

            foreach(var fact in entry.Facts)
            {
                switch(fact.Mode())
                {
                    case BFMode.Player:
                        if (m_ObservedFacts.Contains(fact.Id()))
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
            BestiaryDesc entry = Services.Assets.Bestiary.Get(inEntityId);
            int count = 0;

            foreach(var fact in entry.Facts)
            {
                switch(fact.Mode())
                {
                    case BFMode.Player:
                        if (m_ObservedFacts.Contains(fact.Id()))
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
                m_HasChanges = true;
                m_GraphedFacts.Remove(inFactId);
                Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.RemovedFact, inFactId));
                return true;
            }

            return false;
        }

        #endregion // Facts

        #region Graphed

        public IEnumerable<StringHash32> GraphedFacts()
        {
            return m_GraphedFacts;
        }

        public bool AddFactToGraph(StringHash32 inFactId)
        {
            RegisterFact(inFactId);

            if (m_GraphedFacts.Contains(inFactId))
                return false;
            
            m_HasChanges = true;
            m_GraphedFacts.Add(inFactId);
            Services.Events.Dispatch(GameEvents.ModelUpdated, inFactId);
            return true;
        }

        public bool IsFactGraphed(StringHash32 inFactId)
        {
            Assert.True(Services.Assets.Bestiary.HasFactWithId(inFactId), "Fact with id '{0}' does not exist", inFactId);

            return m_GraphedFacts.Contains(inFactId);
        }

        /// <summary>
        /// Retrieves all observed/assumed but ungraphed facts
        /// </summary>
        public int GetUngraphedFacts(ICollection<StringHash32> outFacts)
        {
            BestiaryDB db = Services.Assets.Bestiary;
            
            BestiaryDesc desc;
            int count = 0;
            foreach(var entityId in m_ObservedEntities)
            {
                desc = db.Get(entityId);
                if (!desc.HasCategory(BestiaryDescCategory.Critter))
                    continue;

                foreach(var assumed in desc.AssumedFacts)
                {
                    if (!m_GraphedFacts.Contains(assumed.Id()))
                    {
                        outFacts.Add(assumed.Id());
                        count++;
                    }
                }
            }

            BFBase fact;
            foreach(var factId in m_ObservedFacts)
            {
                fact = db.Fact(factId);
                if (!fact.Parent().HasCategory(BestiaryDescCategory.Critter))
                    continue;
                
                if (!m_GraphedFacts.Contains(factId))
                {
                    outFacts.Add(factId);
                    count++;
                }
            }

            return count;
        }

        #endregion // Graphed

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 2; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32ProxySet("allEntities", ref m_ObservedEntities);
            ioSerializer.UInt32ProxySet("allFacts", ref m_ObservedFacts);
            ioSerializer.UInt32ProxySet("graphedFacts", ref m_GraphedFacts);
        }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        #endregion // IProfileChunk
    }
}