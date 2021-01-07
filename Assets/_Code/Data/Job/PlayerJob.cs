using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public class PlayerJob
    {
        private StringHash32 m_JobId;
        private PlayerJobStatus m_Status;

        private JobDesc m_CachedJob;

        public PlayerJob() { }
        public PlayerJob(StringHash32 inJobId)
        {
            m_JobId = inJobId;
        }

        public StringHash32 JobId { get { return m_JobId; } }
        public JobDesc Job
        {
            get
            {
                if (m_CachedJob == null)
                {
                    m_CachedJob = Services.Assets.Jobs.Get(m_JobId);
                }

                return m_CachedJob;
            }
        }

        public PlayerJobStatus Status() { return m_Status; }

        public bool IsInProgress()
        {
            return m_Status == PlayerJobStatus.InProgress;
        }

        public void Reset()
        {
            m_Status = PlayerJobStatus.InProgress;
        }

        public bool Begin()
        {
            if (m_Status == PlayerJobStatus.NotStarted)
            {
                m_Status = PlayerJobStatus.InProgress;
                return true;
            }

            return false;
        }

        public bool Complete()
        {
            if (m_Status == PlayerJobStatus.InProgress)
            {
                m_Status = PlayerJobStatus.Completed;
                return true;
            }

            return false;
        }
    }

    public enum PlayerJobStatus : byte
    {
        NotStarted,
        InProgress,
        Completed
    }
}