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

        // scheduling

        private RingBuffer<GTEventDate> m_ScheduledEvents = new RingBuffer<GTEventDate>(32, RingBufferMode.Expand);

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

            m_ScheduledEvents.Clear();
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

            Services.Events.QueueForDispatch(GameEvents.ScriptNodeSeen, inId);
        }

        #endregion // Node Visits

        #region Scheduling

        /// <summary>
        /// Schedules an event at the given time.
        /// </summary>
        public void ScheduleEvent(StringHash32 inId, GTDate inTime)
        {
            for(int i = 0, len = m_ScheduledEvents.Count; i < len; i++)
            {
                ref GTEventDate evt = ref m_ScheduledEvents[i];
                if (evt.Id == inId)
                {
                    evt.Time = inTime;
                    evt.Data = Variant.Null;
                    return;
                }
            }

            GTEventDate newEvt = new GTEventDate();
            newEvt.Id = inId;
            newEvt.Time = inTime;
            m_ScheduledEvents.PushBack(newEvt);
        }

        /// <summary>
        /// Schedules an event at the given time.
        /// </summary>
        public void ScheduleEvent(StringHash32 inId, GTDate inTime, Variant inData)
        {
            for(int i = 0, len = m_ScheduledEvents.Count; i < len; i++)
            {
                ref GTEventDate evt = ref m_ScheduledEvents[i];
                if (evt.Id == inId)
                {
                    evt.Time = inTime;
                    evt.Data = inData;
                    return;
                }
            }

            GTEventDate newEvt = new GTEventDate();
            newEvt.Id = inId;
            newEvt.Time = inTime;
            newEvt.Data = inData;
            m_ScheduledEvents.PushBack(newEvt);
        }

        /// <summary>
        /// Returns if an event with the given id is scheduled.
        /// </summary>
        public bool IsEventScheduled(StringHash32 inId)
        {
            for(int i = 0, len = m_ScheduledEvents.Count; i < len; i++)
            {
                ref GTEventDate evt = ref m_ScheduledEvents[i];
                if (evt.Id == inId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the time until the scheduled event occurs.
        /// </summary>
        public GTTimeSpan TimeUntilScheduled(StringHash32 inId)
        {
            return TimeUntilScheduled(inId, Services.Time.Current);
        }

        /// <summary>
        /// Returns the time until the scheduled event occurs, relative to the given reference date.
        /// </summary>
        public GTTimeSpan TimeUntilScheduled(StringHash32 inId, GTDate inReference)
        {
            for(int i = 0, len = m_ScheduledEvents.Count; i < len; i++)
            {
                ref GTEventDate evt = ref m_ScheduledEvents[i];
                if (evt.Id == inId)
                {
                    return evt.Time - inReference;
                }
            }

            return new GTTimeSpan(long.MaxValue);
        }

        /// <summary>
        /// Attempts to return the extra data for the given scheduled event.
        /// </summary>
        public bool TryGetScheduledEventData(StringHash32 inId, out Variant outData)
        {
            for(int i = 0, len = m_ScheduledEvents.Count; i < len; i++)
            {
                ref GTEventDate evt = ref m_ScheduledEvents[i];
                if (evt.Id == inId)
                {
                    outData = evt.Data;
                    return true;
                }
            }

            outData = default(Variant);
            return false;
        }

        /// <summary>
        /// Removes the scheduled event with the given id.
        /// </summary>
        public bool ClearScheduledEvent(StringHash32 inId)
        {
            for(int i = 0, len = m_ScheduledEvents.Count; i < len; i++)
            {
                ref GTEventDate evt = ref m_ScheduledEvents[i];
                if (evt.Id == inId)
                {
                    m_ScheduledEvents.FastRemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion // Scheduling

        private void OnTableUpdated(NamedVariant inVariant)
        {
            m_HasChanges = true;
        }

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 2; } }

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
                ioSerializer.ObjectArray("scheduled", ref m_ScheduledEvents);
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