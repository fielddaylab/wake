using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua.Profile
{
    public class BestiaryData : ISerializedObject, ISerializedVersion
    {
        private HashSet<StringHash32> m_ObservedEntities = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_ObservedFacts = new HashSet<StringHash32>();
        private List<PlayerFactParams> m_Facts = new List<PlayerFactParams>();

        #region Observed Entities

        public bool HasEntity(StringHash32 inEntityId)
        {
            return m_ObservedEntities.Contains(inEntityId);
        }

        public bool RegisterEntity(StringHash32 inEntityId)
        {
            if (m_ObservedEntities.Add(inEntityId))
            {
                Services.Events.Dispatch(GameEvents.BestiaryUpdated);
                return true;
            }

            return false;
        }

        public IEnumerable<BestiaryDesc> GetEntities(BestiaryDescCategory inCategory)
        {
            foreach(var entity in m_ObservedEntities)
            {
                BestiaryDesc desc = Services.Assets.Bestiary.Get(entity);
                if (desc.Category() == inCategory)
                    yield return desc;
            }
        }

        #endregion // Observed Entities

        #region Observed Behaviors

        public bool HasBaseFact(StringHash32 inBehaviorId)
        {
            return m_ObservedFacts.Contains(inBehaviorId);
        }

        public bool RegisterBaseFact(StringHash32 inBehaviorId)
        {
            if (m_ObservedFacts.Add(inBehaviorId))
            {
                var fact = AddFact(inBehaviorId).Fact;
                m_ObservedEntities.Add(fact.Parent().Id());
                Services.Events.Dispatch(GameEvents.BestiaryUpdated);
                return true;
            }

            return false;
        }

        #endregion // Observed Behaviors

        #region Facts

        public IEnumerable<PlayerFactParams> GetFactsForEntity(StringHash32 inEntityId)
        {
            foreach(var fact in m_Facts)
            {
                if (fact.Fact.Parent().Id() == inEntityId)
                    yield return fact;
            }
        }

        public IEnumerable<PlayerFactParams> GetFactsForBaseFact(StringHash32 inFactId)
        {
            foreach(var fact in m_Facts)
            {
                if (fact.FactId == inFactId)
                    yield return fact;
            }
        }

        public PlayerFactParams AddFact(StringHash32 inBaseFact)
        {
            PlayerFactParams fact = new PlayerFactParams(inBaseFact);
            m_Facts.Add(fact);
            return fact;
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