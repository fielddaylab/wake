using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile
{
    public class JobsData : ISerializedObject, ISerializedVersion, ISerializedCallbacks
    {
        // Serialized
        private List<PlayerJob> m_JobStatuses = new List<PlayerJob>();
        private HashSet<StringHash32> m_CompletedJobs = new HashSet<StringHash32>();
        private StringHash32 m_CurrentJobId;

        // Non-Serialized
        private PlayerJob m_CurrentJob;

        private readonly PlayerJob m_TempJob = new PlayerJob();

        #region Current Job

        public PlayerJob CurrentJob { get { return m_CurrentJob; } }
        public StringHash32 CurrentJobId { get { return m_CurrentJobId; } }

        public bool SetCurrentJob(StringHash32 inJobId)
        {
            if (m_CurrentJobId == inJobId)
                return false;

            Assert.False(m_CompletedJobs.Contains(inJobId), "Cannot use completed job as active job");

            if (m_CurrentJob != null)
            {
                m_CurrentJob.SetAsNotActive();
            }

            m_CurrentJobId = inJobId;
            m_CurrentJob = GetProgress(inJobId, true);

            m_CurrentJob.SetAsActive();
            Services.Events.Dispatch(GameEvents.JobSwitched, inJobId);
            return true;
        }

        #endregion // Current Job

        #region Progress

        public PlayerJob GetProgress(StringHash32 inJobId)
        {
            return GetProgress(inJobId, false);
        }

        private PlayerJob GetProgress(StringHash32 inJobId, bool inbCreate)
        {
            if (inJobId == StringHash32.Null)
                return null;

            if (m_CompletedJobs.Contains(inJobId))
            {
                m_TempJob.SetAsTemp(inJobId, PlayerJobStatus.Completed);
                return m_TempJob;
            }

            for(int i = 0; i < m_JobStatuses.Count; ++i)
            {
                if (m_JobStatuses[i].JobId == inJobId)
                    return m_JobStatuses[i];
            }

            if (inbCreate)
            {
                PlayerJob job = new PlayerJob(inJobId);
                job.Begin();
                m_JobStatuses.Add(job);
                Services.Events.Dispatch(GameEvents.JobStarted, inJobId);
                return job;
            }

            m_TempJob.SetAsTemp(inJobId, PlayerJobStatus.NotStarted);
            return m_TempJob;
        }

        public bool IsInProgress(StringHash32 inJobId)
        {
            PlayerJob job = GetProgress(inJobId);
            return job.IsInProgress();
        }

        public bool IsStarted(StringHash32 inJobId)
        {
            return IsComplete(inJobId) || IsInProgress(inJobId);
        }

        public ListSlice<PlayerJob> InProgressJobs()
        {
            return m_JobStatuses;
        }

        public IEnumerable<StringHash32> CompletedJobIds()
        {
            return m_CompletedJobs;
        }

        #endregion // Progress

        #region Completion

        public bool MarkComplete(PlayerJob inJob)
        {
            Assert.NotNull(inJob);

            if (inJob.Complete())
            {
                bool bIsCurrent = m_CurrentJob == inJob;

                m_CompletedJobs.Add(inJob.JobId);
                m_JobStatuses.FastRemove(inJob);

                m_CurrentJobId = StringHash32.Null;
                m_CurrentJob = null;
                Services.Events.Dispatch(GameEvents.JobCompleted, inJob.JobId);

                if (bIsCurrent)
                {
                    if (m_JobStatuses.Count > 0)
                    {
                        m_CurrentJob = RNG.Instance.Choose(m_JobStatuses);
                        m_CurrentJobId = m_CurrentJob.JobId;
                        m_CurrentJob.SetAsActive();
                        Services.Events.Dispatch(GameEvents.JobSwitched, m_CurrentJobId);
                    }
                    else
                    {
                        m_CurrentJob = null;
                        m_CurrentJobId = StringHash32.Null;
                        Services.Events.Dispatch(GameEvents.JobSwitched, StringHash32.Null);
                    }
                }
                return true;
            }

            return false;
        }

        public bool IsComplete(StringHash32 inJobId)
        {
            return m_CompletedJobs.Contains(inJobId);
        }

        #endregion // Completion

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // TODO: Implement
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            m_CurrentJob = GetProgress(m_CurrentJobId, true);
        }

        #endregion // ISerializedData
    }
}