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
            return m_ObservedEntities.Add(inEntityId);
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
                m_Facts.Add(new PlayerFactParams(inBehaviorId));
                return true;
            }

            return false;
        }

        #endregion // Observed Behaviors

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // TODO: Implement
        }

        #endregion // ISerializedData
    }
}