using BeauData;
using BeauUtil;

namespace Aqua
{
    public class PlayerJob : ISerializedObject
    {
        private StringHash32 m_JobId;
        private PlayerJobStatus m_Status;

        private JobDesc m_CachedJob;

        public PlayerJob() { }
        public PlayerJob(StringHash32 inJobId)
        {
            m_JobId = inJobId;
        }

        public StringHash32 JobId
        {
            get { return m_JobId; }
            private set
            {
                if (m_JobId != null)
                {
                    m_JobId = value;
                    m_CachedJob = null;
                }
            }
        }
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

        #region Checks

        public bool IsStarted()
        {
            return m_Status != PlayerJobStatus.NotStarted;
        }

        public bool IsInProgress()
        {
            return m_Status == PlayerJobStatus.InProgress || m_Status == PlayerJobStatus.Active;
        }

        public bool IsActive()
        {
            return m_Status == PlayerJobStatus.Active;
        }

        public bool IsComplete()
        {
            return m_Status == PlayerJobStatus.Completed;
        }

        #endregion // Checks

        #region Internal

        internal void Reset()
        {
            m_Status = PlayerJobStatus.InProgress;
        }

        internal bool Begin()
        {
            if (m_Status == PlayerJobStatus.NotStarted)
            {
                m_Status = PlayerJobStatus.Active;
                return true;
            }

            return false;
        }

        internal bool SetAsActive()
        {
            if (m_Status == PlayerJobStatus.InProgress)
            {
                m_Status = PlayerJobStatus.Active;
                return true;
            }

            return false;
        }

        internal bool SetAsNotActive()
        {
            if (m_Status == PlayerJobStatus.Active)
            {
                m_Status = PlayerJobStatus.InProgress;
                return true;
            }

            return false;
        }

        internal bool Complete()
        {
            if (m_Status == PlayerJobStatus.InProgress || m_Status == PlayerJobStatus.Active)
            {
                m_Status = PlayerJobStatus.Completed;
                return true;
            }

            return false;
        }

        internal void SetAsTemp(StringHash32 inHash32, PlayerJobStatus inStatus)
        {
            m_Status = inStatus;
            m_JobId = inHash32;
            m_CachedJob = null;
        }

        #endregion // Internal

        #region ISerializedObject

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref m_JobId);
            ioSerializer.Enum("status", ref m_Status);
        }

        #endregion // ISerializedObject
    }

    public enum PlayerJobStatus : byte
    {
        NotStarted,
        InProgress,
        Active,
        Completed
    }
}