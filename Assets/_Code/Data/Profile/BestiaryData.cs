using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Profile
{
    public class BestiaryData : ISerializedObject, ISerializedVersion
    {
        private HashSet<StringHash32> m_ObservedEntities = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_ObservedFacts = new HashSet<StringHash32>();
        private List<PlayerFactParams> m_Facts = new List<PlayerFactParams>();

        [NonSerialized] private bool m_FactListDirty = true;

        #region Observed Entities

        public bool HasEntity(StringHash32 inEntityId)
        {
            return m_ObservedEntities.Contains(inEntityId);
        }

        public bool RegisterEntity(StringHash32 inEntityId)
        {
            if (m_ObservedEntities.Add(inEntityId))
            {
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

        #endregion // Observed Entities

        #region Facts

        public bool HasFact(StringHash32 inFactId)
        {
            return m_ObservedFacts.Contains(inFactId) || Services.Assets.Bestiary.IsAutoFact(inFactId);
        }

        public bool RegisterFact(StringHash32 inFactId)
        {
            return RegisterFact(inFactId, out PlayerFactParams temp);
        }

        public bool RegisterFact(StringHash32 inFactId, out PlayerFactParams outParams)
        {
            if (Services.Assets.Bestiary.IsAutoFact(inFactId))
            {
                var fact = Services.Assets.Bestiary.Fact(inFactId);
                if (fact.Mode() == BFMode.Always)
                    outParams = PlayerFactParams.Wrap(fact);
                else
                    outParams = null;
                return false;
            }

            if (m_ObservedFacts.Add(inFactId))
            {
                var factParams = AddFact(inFactId);
                var fact = factParams.Fact; 
                m_ObservedEntities.Add(fact.Parent().Id());
                Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Fact, inFactId));
                outParams = factParams;
                return true;
            }

            SortFacts();
            m_Facts.TryBinarySearch(inFactId, out outParams);
            Assert.NotNull(outParams);
            return false;
        }

        public PlayerFactParams GetFact(StringHash32 inFactId)
        {
            if (Services.Assets.Bestiary.IsAutoFact(inFactId))
            {
                return PlayerFactParams.Wrap(Services.Assets.Bestiary.Fact(inFactId));
            }

            SortFacts();
            
            PlayerFactParams p;
            if (!m_Facts.TryBinarySearch(inFactId, out p))
            {
                Debug.LogErrorFormat("[BestiaryData] No fact with id '{0}' has been registered", inFactId.ToDebugString());
            }

            return p;
        }

        public IEnumerable<PlayerFactParams> GetFactsForEntity(StringHash32 inEntityId)
        {
            BestiaryDesc entry = Services.Assets.Bestiary.Get(inEntityId);

            foreach(var fact in entry.AssumedFacts)
            {
                yield return PlayerFactParams.Wrap(fact);
            }

            foreach(var fact in m_Facts)
            {
                if (fact.Fact.Parent() == entry)
                    yield return fact;
            }
        }

        private PlayerFactParams AddFact(StringHash32 inBaseFact)
        {
            PlayerFactParams fact = new PlayerFactParams(inBaseFact);
            m_Facts.Add(fact);
            m_FactListDirty = true;
            return fact;
        }

        private void SortFacts()
        {
            if (!m_FactListDirty)
                return;

            m_Facts.SortByKey<StringHash32, PlayerFactParams, PlayerFactParams>();
            m_FactListDirty = false;
        }

        #endregion // Facts

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // TODO: Implement
        }

        #endregion // ISerializedData
    }
}