using System;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;

namespace Aqua
{
    public class PlayerFactParams : IKeyValuePair<StringHash32, PlayerFactParams>, IComparable<PlayerFactParams>, ISerializedObject
    {
        [NonSerialized] private BFBase m_CachedFact;
        [NonSerialized] private bool m_Locked;
        
        // base fact id
        private StringHash32 m_FactId;
        private PlayerFactFlags m_Flags;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, PlayerFactParams>.Key { get { return m_FactId; } }

        PlayerFactParams IKeyValuePair<StringHash32, PlayerFactParams>.Value { get { return this; } }

        #endregion // KeyValue

        public StringHash32 FactId { get { return m_FactId; } }
        
        public BFBase Fact
        {
            get
            {
                if (m_CachedFact.IsReferenceNull())
                {
                    m_CachedFact = Services.Assets.Bestiary.Fact(m_FactId);
                }

                return m_CachedFact;
            }
        }

        public PlayerFactParams() { }

        public PlayerFactParams(StringHash32 inFactId, PlayerFactFlags inFlags = 0)
        {
            m_FactId = inFactId;
            m_Flags = inFlags;
        }

        internal PlayerFactParams(BFBase inFact)
        {
            m_FactId = inFact.Id();
            m_CachedFact = inFact;
            m_Flags = PlayerFactFlags.All;
            m_Locked = true;
        }

        public PlayerFactFlags Flags
        {
            get { return m_Flags; }
        }

        public bool Has(PlayerFactFlags inFlags)
        {
            return (m_Flags & inFlags) != 0;
        }

        public bool HasAll(PlayerFactFlags inFlags)
        {
            return (m_Flags & inFlags) == inFlags;
        }

        public bool Add(PlayerFactFlags inFlags)
        {
            Assert.False(m_Locked, "PlayerFactParams is read-only");

            PlayerFactFlags prev = m_Flags;
            m_Flags |= inFlags;
            if (prev != m_Flags)
            {
                Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Fact, m_FactId));
                return true;
            }

            return false;
        }

        public bool Remove(PlayerFactFlags inFlags)
        {
            Assert.False(m_Locked, "PlayerFactParams is read-only");

            PlayerFactFlags prev = m_Flags;
            m_Flags &= ~inFlags;
            
            if (prev != m_Flags)
            {
                Services.Events.Dispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Fact, m_FactId));
                return true;
            }

            return false;
        }

        static public PlayerFactParams Wrap(BFBase inFact)
        {
            return inFact.GetWrapper();
        }

        #region IComparable

        int IComparable<PlayerFactParams>.CompareTo(PlayerFactParams other)
        {
            return Fact.CompareTo(other.Fact);
        }

        #endregion // IComparable

        #region ISerializedObject

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref m_FactId);
            ioSerializer.Enum("flags", ref m_Flags);
        }

        #endregion // ISerializedObject
    }
}