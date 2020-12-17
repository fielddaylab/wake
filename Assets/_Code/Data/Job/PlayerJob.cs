using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class PlayerJob
    {
        private StringHash32 m_JobId;
        private PlayerJobStatus m_Status;

        public StringHash32 JobId { get { return m_JobId; } }
    }

    public enum PlayerJobStatus
    {
        NotStarted,
        InProgress,
        Rejected,
        Completed
    }
}