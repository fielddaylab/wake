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
            public Sprite LockedIcon;
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
        [NonSerialized] private JobStatusFlags m_Status;
        [NonSerialized] private JobProgressCategory m_Group;

        public JobDesc Job { get { return m_Job; } }
        public JobStatusFlags Status { get { return m_Status; } }
        public JobProgressCategory Group { get { return m_Group; } }
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

        public void Populate(JobDesc inJob, JobStatusFlags inStatus)
        {
            m_Job = inJob;
            m_Status = inStatus;
            m_Group = PlayerJob.StatusToCategory(inStatus);

            m_NameLabel.SetText(inJob.NameId());
            
            if (m_PosterLabel)
                m_PosterLabel.SetText(inJob.PosterId());

            Sprite jobIcon = null; // TODO: REIMPLEMENT
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

        public bool UpdateStatus(JobStatusFlags inStatus, in ButtonAppearanceConfig inConfig)
        {
            if (m_Status != inStatus)
            {
                m_Status = inStatus;
                m_Group = PlayerJob.StatusToCategory(inStatus);
                switch(m_Group)
                {
                    case JobProgressCategory.Completed:
                        {
                            m_Icon.gameObject.SetActive(true);
                            m_Icon.sprite = inConfig.CompletedIcon;
                            break;
                        }
                    
                    case JobProgressCategory.Active:
                        {
                            m_Icon.gameObject.SetActive(true);
                            m_Icon.sprite = inConfig.ActiveIcon;
                            break;
                        }

                    case JobProgressCategory.Locked:
                        {
                            m_Icon.gameObject.SetActive(true);
                            m_Icon.sprite = inConfig.LockedIcon;
                            break;
                        }

                    default:
                        {
                            Sprite icon = null; // TODO: REIMPLEMENT
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