using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using BeauPools;

namespace Aqua.JobBoard
{
    public class JobButton : MonoBehaviour, IPoolAllocHandler
    {
        [Serializable]
        public struct ButtonAppearanceConfig
        {
            public Sprite ActiveIcon;
            public Sprite CompletedIcon;
        }

        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_NameLabel = null;
        [SerializeField] private LocText m_PosterLabel = null;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private Action<JobButton> m_OnSelected;
        [NonSerialized] private JobDesc m_Job;
        [NonSerialized] private PlayerJobStatus m_Status;

        public JobDesc Job { get { return m_Job; } }
        public PlayerJobStatus Status { get { return m_Status; } }
        public Transform Transform { get { return this.CacheComponent(ref m_Transform); } }

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggle);
        }

        public void Initialize(ToggleGroup inGroup, Action<JobButton> inSelectedCallback)
        {
            m_Toggle.group = inGroup;
            m_OnSelected = inSelectedCallback;
        }

        public void Populate(JobDesc inJob, PlayerJobStatus inStatus)
        {
            m_Job = inJob;
            m_Status = inStatus;

            m_NameLabel.SetText(inJob.NameId());
            
            if (m_PosterLabel)
                m_PosterLabel.SetText(inJob.PosterId());

            var jobIcon = inJob.Icon();
            if (jobIcon != null)
            {
                m_Icon.sprite = jobIcon;
                m_Icon.gameObject.SetActive(true);
            }
            else
            {
                m_Icon.gameObject.SetActive(false);
                m_Icon.sprite = null;
            }
        }

        public bool UpdateStatus(PlayerJobStatus inStatus, in ButtonAppearanceConfig inConfig)
        {
            if (m_Status != inStatus)
            {
                m_Status = inStatus;
                switch(inStatus)
                {
                    case PlayerJobStatus.Completed:
                        {
                            m_Icon.sprite = inConfig.CompletedIcon;
                            break;
                        }
                    
                    case PlayerJobStatus.Active:
                        {
                            m_Icon.sprite = inConfig.ActiveIcon;
                            break;
                        }

                    default:
                        {
                            var icon = m_Job.Icon();
                            m_Icon.sprite = icon;
                            m_Icon.gameObject.SetActive(icon);
                            break;
                        }
                }
                return true;
            }

            return false;
        }

        private void OnToggle(bool inbValue)
        {
            if (inbValue)
                m_OnSelected(this);
        }

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Job = null;
        }
    }
}