using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;
using Aqua.Scripting;

namespace Aqua.Profile
{
    public class ScriptingData : ISerializedObject, ISerializedVersion
    {
        // Tables

        public VariantTable GlobalTable = new VariantTable("global");
        public VariantTable JobsTable = new VariantTable("jobs");

        public VariantTable PlayerTable = new VariantTable("player");
        public VariantTable PartnerTable = new VariantTable("partner");

        // Conversation History

        private HashSet<StringHash32> m_TrackedVisitedNodes = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_TrackedVisitedNodesForSession = new HashSet<StringHash32>();
        private RingBuffer<StringHash32> m_RecentNodeHistory = new RingBuffer<StringHash32>(32, RingBufferMode.Overwrite);

        public IReadOnlyCollection<StringHash32> RecentNodeHistory { get { return m_RecentNodeHistory; } }
        public IReadOnlyCollection<StringHash32> ProfileNodeHistory { get { return m_TrackedVisitedNodes; } }
        public IReadOnlyCollection<StringHash32> SessionNodeHistory { get { return m_TrackedVisitedNodesForSession; } }

        public void Reset()
        {
            GlobalTable.Clear();
            PlayerTable.Clear();
            PartnerTable.Clear();

            m_TrackedVisitedNodes.Clear();
            m_TrackedVisitedNodesForSession.Clear();
            m_RecentNodeHistory.Clear();
        }

        #region Node Visits

        public bool HasSeen(StringHash32 inNodeId, PersistenceLevel inPersist = PersistenceLevel.Untracked)
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

        public bool HasRecentlySeen(StringHash32 inNodeId, int inDuration)
        {
            for(int i = 0; i < m_RecentNodeHistory.Count && i < inDuration; ++i)
            {
                if (m_RecentNodeHistory[i] == inNodeId)
                    return true;
            }

            return false;
        }

        public void RecordNodeVisit(StringHash32 inId, PersistenceLevel inPersist = PersistenceLevel.Untracked)
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