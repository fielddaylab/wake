using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;
using Aqua.Scripting;

namespace Aqua.Profile
{
    public class ScriptingData : IProfileChunk, ISerializedVersion
    {
        // Tables

        public VariantTable GlobalTable = new VariantTable("global");
        public VariantTable JobsTable = new VariantTable("jobs");
        public VariantTable WorldTable = new VariantTable("world");

        public VariantTable PlayerTable = new VariantTable("player");
        public VariantTable PartnerTable = new VariantTable("partner");

        // Conversation History

        private HashSet<StringHash32> m_TrackedVisitedNodes = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_TrackedVisitedNodesForSession = new HashSet<StringHash32>();
        private RingBuffer<StringHash32> m_RecentNodeHistory = new RingBuffer<StringHash32>(32, RingBufferMode.Overwrite);

        private uint m_ActIndex = 0;

        private bool m_HasChanges;

        public uint ActIndex
        {
            get { return m_ActIndex; }
            set
            {
                if (m_ActIndex != value)
                {
                    m_ActIndex = value;
                    m_HasChanges = true;
                    Services.Events.Dispatch(GameEvents.ActChanged, value);
                }
            }
        }

        public IReadOnlyCollection<StringHash32> RecentNodeHistory { get { return m_RecentNodeHistory; } }
        public IReadOnlyCollection<StringHash32> ProfileNodeHistory { get { return m_TrackedVisitedNodes; } }
        public IReadOnlyCollection<StringHash32> SessionNodeHistory { get { return m_TrackedVisitedNodesForSession; } }

        public ScriptingData()
        {
            GlobalTable.OnUpdated += OnTableUpdated;
            JobsTable.OnUpdated += OnTableUpdated;
            PlayerTable.OnUpdated += OnTableUpdated;
            PartnerTable.OnUpdated += OnTableUpdated;
            WorldTable.OnUpdated += OnTableUpdated;
        }

        public void Reset()
        {
            GlobalTable.Clear();
            PlayerTable.Clear();
            PartnerTable.Clear();
            JobsTable.Clear();
            WorldTable.Clear();

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
                        m_HasChanges = true;
                        break;
                    }

                case PersistenceLevel.Session:
                    {
                        m_TrackedVisitedNodesForSession.Add(inId);
                        break;
                    }
            }

            Services.Events.Queue(GameEvents.ScriptNodeSeen, inId);
        }

        #endregion // Node Visits

        private void OnTableUpdated(NamedVariant inVariant)
        {
            m_HasChanges = true;
        }

        #region IProfileChunk

        // v2: added world table and scheduled events
        // v3: removed scheduled events
        ushort ISerializedVersion.Version { get { return 3; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("actIndex", ref m_ActIndex);
            ioSerializer.UInt32ProxySet("visited", ref m_TrackedVisitedNodes);

            ioSerializer.Object("globals", ref GlobalTable);
            ioSerializer.Object("jobs", ref JobsTable);
            ioSerializer.Object("player", ref PlayerTable);
            ioSerializer.Object("partner", ref PartnerTable);

            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Object("world", ref WorldTable);
                if (ioSerializer.ObjectVersion < 3)
                {
                    WaterPropertyBlockU8[] v = null;
                    ioSerializer.ObjectArray("scheduled", ref v);
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

        #endregion // IProfileChunk
    }
}