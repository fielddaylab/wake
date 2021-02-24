using UnityEngine;
using Aqua;
using UnityEngine.UI;
using System;

namespace ProtoAqua.JobBoard
{
    public class JobInfo : MonoBehaviour
    {
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
            
            m_JobInfo.Populate(inJob);
            UpdateStatus(inStatus);
        }

        public void UpdateStatus(PlayerJobStatus inStatus)
        {
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
                        m_ActionButtonLabel.SetText("Set as Active");
                        m_ActionButton.gameObject.SetActive(true);
                        break;
                    }

                case PlayerJobStatus.NotStarted:
                    {
                        m_ActionButtonLabel.SetText("Accept");
                        m_ActionButton.gameObject.SetActive(true);
                        break;
                    }
            }
        }
    }    
}