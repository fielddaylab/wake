using UnityEngine;
using Aqua;
using UnityEngine.UI;
using System;
using BeauUtil;

namespace ProtoAqua.JobBoard
{
    public class JobInfo : MonoBehaviour
    {
        static private readonly StringHash32 Label_AcceptJob = "ui.jobBoard.start.label";
        static private readonly StringHash32 Label_ActivateJob = "ui.jobBoard.setActive.label";

        #region Inspector

        [SerializeField] private JobInfoDisplay m_JobInfo = null;
        [SerializeField] private Button m_ActionButton = null;
        [SerializeField] private LocText m_ActionButtonLabel = null;
        [SerializeField] private RectTransform m_NoJobDisplay = null;

        #endregion // Inspector

        public Button.ButtonClickedEvent OnActionClicked
        {
            get { return m_ActionButton.onClick; }
        }

        public void Clear()
        {
            m_JobInfo.gameObject.SetActive(false);
            m_NoJobDisplay.gameObject.SetActive(true);
        }

        public void Populate(JobDesc inJob, PlayerJobStatus inStatus)
        {
            m_NoJobDisplay.gameObject.SetActive(false);
            m_JobInfo.gameObject.SetActive(true);
            
            m_JobInfo.Populate(inJob, inStatus);
            UpdateStatus(inJob, inStatus, false);
        }

        public void UpdateStatus(JobDesc inJob, PlayerJobStatus inStatus, bool inbUpdateInfo = true)
        {
            if (inbUpdateInfo)
                m_JobInfo.UpdateStatus(inJob, inStatus);

            switch(inStatus)
            {
                case PlayerJobStatus.Completed:
                case PlayerJobStatus.Active:
                    {
                        m_ActionButton.gameObject.SetActive(false);
                        break;
                    }

                case PlayerJobStatus.InProgress:
                    {
                        m_ActionButtonLabel.SetText(Label_ActivateJob);
                        m_ActionButton.gameObject.SetActive(true);
                        break;
                    }

                case PlayerJobStatus.NotStarted:
                    {
                        m_ActionButtonLabel.SetText(Label_AcceptJob);
                        m_ActionButton.gameObject.SetActive(true);
                        break;
                    }
            }
        }
    }    
}