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

        #region Current Job

        public bool SetCurrentJob(StringHash32 inJobId)
        {
            if (m_CurrentJobId == inJobId)
                return false;

            m_CurrentJobId = inJobId;
            m_CurrentJob = GetProgress(inJobId, true);
            return true;
        }

        #endregion // Current Job

        #region Progress

        public PlayerJob GetProgress(StringHash32 inJobId, bool inbCreate = false)
        {
            if (inJobId == StringHash32.Null)
                return null;

            for(int i = 0; i < m_JobStatuses.Count; ++i)
            {
                if (m_JobStatuses[i].JobId == inJobId)
                    return m_JobStatuses[i];
            }

            if (inbCreate)
            {
                PlayerJob job = new PlayerJob(inJobId);
                m_JobStatuses.Add(job);
                return job;
            }

            return null;
        }

        public bool IsInProgress(StringHash32 inJobId)
        {
            PlayerJob job = GetProgress(inJobId);
            return job != null && job.IsInProgress();
        }

        #endregion // Progress

        #region Completion

        public bool MarkComplete(PlayerJob inJob)
        {
            Assert.NotNull(inJob);

            if (inJob.Complete())
            {
                m_CompletedJobs.Add(inJob.JobId);
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
            // TODO: Implement
        }

        #endregion // ISerializedData
    }
}