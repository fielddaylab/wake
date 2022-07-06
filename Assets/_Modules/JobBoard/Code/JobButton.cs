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
        private const int NoIconPaddingRight = 10;
        private const int IconPaddingRight = 28;

        [Serializable]
        public struct ButtonAppearanceConfig
        {
            public Sprite ActiveIcon;
            public Color ActiveColor;
            public Sprite LockedIcon;
            public Color LockedColor;
        }

        #region Inspector

        [SerializeField, Required] private LayoutGroup m_LayoutGroup = null;
        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_NameLabel = null;

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
                            m_Icon.gameObject.SetActive(false);
                            RectOffset off = m_LayoutGroup.padding;
                            off.right = NoIconPaddingRight;
                            m_LayoutGroup.padding = off;
                            break;
                        }
                    
                    case JobProgressCategory.Active:
                        {
                            m_Icon.gameObject.SetActive(true);
                            m_Icon.sprite = inConfig.ActiveIcon;
                            m_Icon.color = inConfig.ActiveColor;
                            RectOffset off = m_LayoutGroup.padding;
                            off.right = IconPaddingRight;
                            m_LayoutGroup.padding = off;
                            break;
                        }

                    case JobProgressCategory.Locked:
                        {
                            m_Icon.gameObject.SetActive(true);
                            m_Icon.sprite = inConfig.LockedIcon;
                            m_Icon.color = inConfig.LockedColor;
                            RectOffset off = m_LayoutGroup.padding;
                            off.right = IconPaddingRight;
                            m_LayoutGroup.padding = off;
                            break;
                        }

                    default:
                        {
                            m_Icon.gameObject.SetActive(false);
                            RectOffset off = m_LayoutGroup.padding;
                            off.right = NoIconPaddingRight;
                            m_LayoutGroup.padding = off;
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
            m_OnSelected = null;
            m_Toggle.SetIsOnWithoutNotify(false);
        }
    }
}