using System;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;

namespace Aqua
{
    public class TimeService : ServiceBehaviour, IPauseable
    {
        private const float RealMinutesPerDay = 60 * 24;
        private const float DefaultMinutesPerDay = 12;

        #region Inspector

        [SerializeField] private float m_MinutesPerDay = DefaultMinutesPerDay;
        [SerializeField] private float m_WorldUpdateDelay = 1f / 20f;

        [Header("Defaults")]
        [SerializeField, Range(0, 23.99f)] private float m_StartingTime = 7f;

        #endregion // Inspector

        [NonSerialized] private float m_CurrentTime;
        [NonSerialized] private GTDate m_FullTime;
        [NonSerialized] private ushort m_TotalDays;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private bool m_TimeCanFlow;

        [NonSerialized] private TimeMode m_TimeMode;
        [NonSerialized] private float m_QueuedAdvance;
        [NonSerialized] private float m_QueuedSet = -1;

        [NonSerialized] private float m_LastUpdatedTime = 0;
        private RingBuffer<ITimeHandler> m_AllHandlers = new RingBuffer<ITimeHandler>(64);
        private RingBuffer<ITimeHandler> m_TickHandlers = new RingBuffer<ITimeHandler>(32);
        private RingBuffer<ITimeHandler> m_TransitionHandlers = new RingBuffer<ITimeHandler>(16);
        private RingBuffer<ITimeHandler> m_DayNightHandlers = new RingBuffer<ITimeHandler>(16);

        public GTDate Current { get { return m_FullTime; } }
        public TimeMode Mode { get { return m_TimeMode; } }

        public GTDate StartingTime() { return new GTDate((ushort) (m_StartingTime * GTDate.TicksPerHour), 0); }

        #region Objects

        /// <summary>
        /// Registers a time-animated object.
        /// This object will be animated by the passage of in-game time.
        /// </summary>
        public void Register(ITimeHandler inObject)
        {
            TimeEvent objectMask = inObject.EventMask();
            if ((objectMask & TimeEvent.Tick) != 0)
                m_TickHandlers.PushBack(inObject);
            if ((objectMask & TimeEvent.DayNightChanged) != 0)
                m_DayNightHandlers.PushBack(inObject);
            if ((objectMask & TimeEvent.Transitioning) != 0)
                m_TransitionHandlers.PushBack(inObject);

            m_AllHandlers.PushFront(inObject);
            
            if (m_TimeCanFlow)
                inObject.OnTimeChanged(m_FullTime);
        }

        /// <summary>
        /// Deregisters a time-animated object.
        /// </summary>
        public void Deregister(ITimeHandler inObject)
        {
            m_AllHandlers.FastRemove(inObject);

            TimeEvent objectMask = inObject.EventMask();
            if ((objectMask & TimeEvent.Tick) != 0)
                m_TickHandlers.FastRemove(inObject);
            if ((objectMask & TimeEvent.DayNightChanged) != 0)
                m_DayNightHandlers.FastRemove(inObject);
            if ((objectMask & TimeEvent.Transitioning) != 0)
                m_TransitionHandlers.FastRemove(inObject);
        }

        #endregion // Objects

        #region Queued

        /// <summary>
        /// Queues time to advance by a certain amount.
        /// </summary>
        public void AdvanceTimeBy(float inHours)
        {
            m_QueuedAdvance = inHours * GTDate.TicksPerHour;
        }

        /// <summary>
        /// Advances time to a specific hour.
        /// </summary>
        public void AdvanceTimeTo(float inHours)
        {
            m_QueuedSet = inHours * GTDate.TicksPerHour;
        }

        /// <summary>
        /// Forces queued changes to process.
        /// </summary>
        public bool ProcessQueuedChanges()
        {
            switch(m_TimeMode)
            {
                case TimeMode.Normal:
                case TimeMode.Realtime:
                    float queuedAdvance = ConsumeQueuedAdvance();
                    if (queuedAdvance > 0)
                    {
                        GTDate prevTime = m_FullTime;
                        m_CurrentTime += queuedAdvance;
                        PostUpdateTime(prevTime);
                        return true;
                    }
                    
                    break;
            }

            return false;
        }

        #endregion // Queued

        #region Updates

        private void LateUpdate()
        {
            if (m_Paused || !m_TimeCanFlow || Services.Script.IsCutscene() || Services.State.IsLoadingScene() || Services.UI.IsTransitioning())
                return;

            GTDate prevTime = m_FullTime;

            switch(m_TimeMode)
            {
                case TimeMode.Normal:
                    AdvanceTime(Time.deltaTime, m_MinutesPerDay);
                    break;

                case TimeMode.Realtime:
                    AdvanceTime(Time.deltaTime, RealMinutesPerDay);
                    break;
            }

            if (m_TimeMode >= TimeMode.FreezeAt0 && m_TimeMode <= TimeMode.FreezeAt22)
            {
                SetTime((int) (m_TimeMode - TimeMode.FreezeAt0) * 2, 0);
            }

            PostUpdateTime(prevTime);
        }

        private float ConsumeQueuedAdvance()
        {
            float ticks = 0;

            if (m_QueuedAdvance > 0)
            {
                ticks += m_QueuedAdvance;
                m_QueuedAdvance = 0;
                m_LastUpdatedTime = -1;
            }
            
            if (m_QueuedSet >= 0)
            {
                float diff = m_QueuedSet - m_CurrentTime;
                if (diff < 0)
                    diff += GTDate.TicksPerDay;
                ticks += diff;
                m_QueuedSet = -1;
                m_LastUpdatedTime = -1;
            }

            return ticks;
        }

        private void AdvanceTime(float inDeltaTime, float inMinutesPerDay)
        {
            float queuedAdvance = ConsumeQueuedAdvance();
            float advancedTicks = queuedAdvance > 0 ? queuedAdvance : GTDate.RealSecondsToTicks(inDeltaTime, inMinutesPerDay);
            m_CurrentTime += advancedTicks;
        }

        private void SetTime(int inHour, int inMinutes)
        {
            m_CurrentTime = GTDate.ClockToTicks(inHour, inMinutes);
            ConsumeQueuedAdvance();
        }

        private void PostUpdateTime(GTDate inPrev)
        {
            while (m_CurrentTime >= GTDate.TicksPerDay)
            {
                m_CurrentTime -= GTDate.TicksPerDay;
                m_TotalDays++;
            }

            GTDate newTime = m_FullTime = new GTDate((ushort) m_CurrentTime, m_TotalDays);

            UpdateTimeElements(inPrev, newTime);
            DispatchTimeChangeEvents(inPrev, newTime);
        }

        private void ForceUpdateElements(GTDate inNow)
        {
            foreach(var obj in m_AllHandlers)
            {
                obj.OnTimeChanged(inNow);
            }
        }

        private void UpdateTimeElements(GTDate inPrev, GTDate inNew)
        {
            if (inPrev == inNew)
                return;

            float now = Time.timeSinceLevelLoad;
            float timeDiff = now - m_LastUpdatedTime;

            bool bTick = timeDiff >= m_WorldUpdateDelay;
            bool bTransitioning = inNew.SubPhase.IsTransitioning();
            bool bTransitionChanged = inPrev.SubPhase.IsTransitioning() != bTransitioning;
            bool bDayNightChanged = inPrev.IsDay != inNew.IsNight;

            if (bDayNightChanged)
            {
                foreach(var obj in m_DayNightHandlers)
                {
                    obj.OnTimeChanged(inNew);
                }
            }

            if (!bTick)
            {
                if (bTransitionChanged)
                {
                    foreach(var obj in m_TransitionHandlers)
                    {
                        obj.OnTimeChanged(inNew);
                    }
                }
            }
            else
            {
                if (bTransitioning || bTransitionChanged)
                {
                    foreach(var obj in m_TransitionHandlers)
                    {
                        obj.OnTimeChanged(inNew);
                    }
                }
                
                foreach(var obj in m_TickHandlers)
                {
                    obj.OnTimeChanged(inNew);
                }
                m_LastUpdatedTime = now;
            }
        }

        static private void DispatchTimeChangeEvents(GTDate inPrev, GTDate inNew)
        {
            if (inNew.Day != inPrev.Day)
            {
                DebugService.Log(LogMask.Time, "[TimeService] Day changed to {0} for time {1}", inNew.Day, inNew);
                Services.Events.Dispatch(GameEvents.TimeDayChanged, inNew);
            }

            if (inNew.Phase != inPrev.Phase)
            {
                DebugService.Log(LogMask.Time, "[TimeService] Day phase changed to {0} for time {1}", inNew.Phase, inNew);
                Services.Events.Dispatch(GameEvents.TimePhaseChanged, inNew);
            }

            if (inNew.IsDay != inPrev.IsDay)
            {
                DebugService.Log(LogMask.Time, "[TimeService] Day day/night changed to {0} for time {1}", inNew.IsDay ? "Day" : "Night", inNew);
                Services.Events.Dispatch(GameEvents.TimeDayNightChanged, inNew);
            }
        }
    
        #endregion // Updates

        #region IService

        protected override void Initialize()
        {
            Services.Events.Register(GameEvents.ProfileUnloaded, OnProfileUnload, this)
                .Register(GameEvents.ProfileLoaded, OnProfileLoaded, this)
                .Register(GameEvents.ProfileStarted, OnProfileStarted, this);

            SceneHelper.OnSceneLoaded += OnSceneLoaded;
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
        }

        #endregion // IService

        #region Handlers

        private void OnProfileUnload()
        {
            m_TimeCanFlow = false;
        }

        private void OnProfileLoaded()
        {
            var mapData = Save.Map;

            m_CurrentTime = mapData.CurrentTime.Ticks;
            m_TotalDays = (ushort) mapData.CurrentTime.Day;
            m_TimeMode = mapData.TimeMode;
            m_TimeCanFlow = false;

            m_FullTime = new GTDate((ushort) m_CurrentTime, m_TotalDays);
        }

        private void OnProfileStarted()
        {
            m_TimeCanFlow = true;
            ForceUpdateElements(m_FullTime);
        }

        private void OnSceneLoaded(SceneBinding _, object __)
        {
            m_LastUpdatedTime = -1;
            if (m_TimeCanFlow)
            {
                ForceUpdateElements(m_FullTime);
            }
        }

        #endregion // Handlers

        #region IPauseable

        bool IPauseable.IsPaused()
        {
            return m_Paused;
        }

        void IPauseable.Pause()
        {
            m_Paused = true;
        }

        void IPauseable.Resume()
        {
            m_Paused = false;
        }

        #endregion // IPauseable
    
        #region Leaf

        static private class LeafIntegration
        {
            private enum SyncMode
            {
                NoSync,
                Sync
            }

            [LeafMember("AdvanceTime"), UnityEngine.Scripting.Preserve]
            static private void AdvanceTime(float inHours, SyncMode inMode = SyncMode.Sync)
            {
                Services.Time.AdvanceTimeBy(inHours);
                if (inMode == SyncMode.Sync)
                    EventApplyTime();
            }

            [LeafMember("SetTime"), UnityEngine.Scripting.Preserve]
            static private void EventSetTime(float inHours, SyncMode inMode = SyncMode.Sync)
            {
                Services.Time.AdvanceTimeTo(inHours);
                if (inMode == SyncMode.Sync)
                    EventApplyTime();
            }

            [LeafMember("SyncTime"), UnityEngine.Scripting.Preserve]
            static private void EventApplyTime()
            {
                Services.Time.ProcessQueuedChanges();
            }
        }

        #endregion // Leaf
    }
}