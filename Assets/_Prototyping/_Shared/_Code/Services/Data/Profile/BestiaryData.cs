using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Profile
{
    public class BestiaryData : ISerializedObject, ISerializedVersion
    {
        private HashSet<StringHash32> m_ObservedBehaviors = new HashSet<StringHash32>();

        #region Observed Behaviors

        public bool WasBehaviorObserved(StringHash32 inBehaviorId)
        {
            return m_ObservedBehaviors.Contains(inBehaviorId);
        }

        public bool RegisterBehaviorObserved(StringHash32 inBehaviorId)
        {
            return m_ObservedBehaviors.Add(inBehaviorId);
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