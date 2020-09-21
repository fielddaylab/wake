using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;
using ProtoAqua.Scripting;

namespace ProtoAqua.Profile
{
    public class ScriptingData : ISerializedObject, ISerializedVersion
    {
        // Tables

        public VariantTable GlobalTable = new VariantTable();

        public VariantTable PlayerTable = new VariantTable();
        public VariantTable PartnerTable = new VariantTable();

        // Conversation History

        private HashSet<StringHash> m_TrackedVisitedNodes = new HashSet<StringHash>();
        private HashSet<StringHash> m_TrackedVisitedNodesForSession = new HashSet<StringHash>();
        private RingBuffer<StringHash> m_RecentNodeHistory = new RingBuffer<StringHash>(32, RingBufferMode.Overwrite);

        #region Node Visits

        public bool HasSeen(StringHash inNodeId, PersistenceLevel inPersist = PersistenceLevel.Untracked)
        {
            switch(inPersist)
            {
                case PersistenceLevel.Untracked:
                default:
                    return m_RecentNodeHistory.Contains(inNodeId);

                case PersistenceLevel.Session:
                    return m_TrackedVisitedNodesForSession.Contains(inNodeId);

                case PersistenceLevel.Profile:
                    return m_TrackedVisitedNodes.Contains(inNodeId);
            }
        }

        public bool HasRecentlySeen(StringHash inNodeId, int inDuration)
        {
            for(int i = 0; i < m_RecentNodeHistory.Count && i < inDuration; ++i)
            {
                if (m_RecentNodeHistory[i] == inNodeId)
                    return true;
            }

            return false;
        }

        public void RecordNodeVisit(StringHash inId, PersistenceLevel inPersist = PersistenceLevel.Untracked)
        {
            m_RecentNodeHistory.PushFront(inId);
            switch(inPersist)
            {
                case PersistenceLevel.Profile:
                    {
                        m_TrackedVisitedNodes.Add(inId);
                        m_TrackedVisitedNodesForSession.Add(inId);
                        break;
                    }

                case PersistenceLevel.Session:
                    {
                        m_TrackedVisitedNodesForSession.Add(inId);
                        break;
                    }
            }
        }

        #endregion // Node Visits

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // TODO: Implement
        }

        #endregion // ISerializedData
    }
}