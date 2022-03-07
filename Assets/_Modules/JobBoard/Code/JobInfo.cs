using UnityEngine;
using Aqua;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using BeauPools;

namespace Aqua.JobBoard
{
    public class JobInfo : MonoBehaviour, IBakedComponent
    {
        static private readonly TextId Label_AcceptJob = "ui.jobBoard.start.label";
        static private readonly TextId Label_ActivateJob = "ui.jobBoard.setActive.label";
        static private readonly TextId Label_LockedJob = "ui.jobBoard.locked.label";

        #region Inspector

        [SerializeField] private JobInfoDisplay m_JobInfo = null;
        [SerializeField] private Button m_ActionButton = null;
        [SerializeField] private LocText m_ActionButtonLabel = null;
        [SerializeField] private RectTransform m_LockedGroup = null;
        [SerializeField] private RectTransform m_NoJobDisplay = null;
        [SerializeField] private RectTransform m_HasRequiredUpgradesGroup = null;

        #endregion // Inspector

        [SerializeField, HideInInspector] private RequiredUpgradeDisplay[] m_UpgradeDisplays = null;

        public Button.ButtonClickedEvent OnActionClicked
        {
            get { return m_ActionButton.onClick; }
        }

        public void Clear()
        {
            m_JobInfo.gameObject.SetActive(false);
            m_NoJobDisplay.gameObject.SetActive(true);
        }

        public void Populate(JobDesc inJob, JobStatusFlags inStatus)
        {
            m_NoJobDisplay.gameObject.SetActive(false);
            m_JobInfo.gameObject.SetActive(true);
            
            m_JobInfo.Populate(inJob, inStatus);
            PopulateRequiredUpgrades(inJob, Save.Current);
            UpdateStatus(inJob, inStatus, false);
        }

        private void PopulateRequiredUpgrades(JobDesc inJob, SaveData inSave)
        {
            var required = inJob.RequiredUpgrades();
            if (required.Length == 0)
            {
                m_HasRequiredUpgradesGroup.gameObject.SetActive(false);
                return;
            }

            using(PooledList<InvItem> has = PooledList<InvItem>.Create())
            using(PooledList<InvItem> missing = PooledList<InvItem>.Create())
            {
                foreach(var upgradeId in required)
                {
                    InvItem upgrade = Assets.Item(upgradeId);
                    if (inSave.Inventory.HasUpgrade(upgradeId))
                    {
                        has.Add(upgrade);
                    }
                    else
                    {
                        missing.Add(upgrade);
                    }
                }

                int allocated = 0;
                for(int i = 0; i < has.Count; i++)
                {
                    PopulateUpgrade(m_UpgradeDisplays[allocated++], has[i], true);
                }

                for(int i = 0; i < missing.Count; i++)
                {
                    PopulateUpgrade(m_UpgradeDisplays[allocated++], missing[i], false);
                }

                for(; allocated < m_UpgradeDisplays.Length; allocated++)
                {
                    m_UpgradeDisplays[allocated].gameObject.SetActive(false);
                }
            }

            m_HasRequiredUpgradesGroup.gameObject.SetActive(true);
        }

        private void PopulateUpgrade(RequiredUpgradeDisplay inDisplay, InvItem inItem, bool inbHas)
        {
            inDisplay.gameObject.SetActive(true);
            inDisplay.Icon.sprite = inItem.Icon();
            inDisplay.Cursor.TooltipId = inItem.NameTextId();
            if (!inbHas)
            {
                inDisplay.Locked.SetActive(true);
                inDisplay.Icon.color = ColorBank.Gray.WithAlpha(0.5f);
            }
            else
            {
                inDisplay.Locked.SetActive(false);
                inDisplay.Icon.color = Color.white;
            }
        }

        public void UpdateStatus(JobDesc inJob, JobStatusFlags inStatus, bool inbUpdateInfo = true)
        {
            if (inbUpdateInfo)
                m_JobInfo.UpdateStatus(inJob, inStatus);

            switch(PlayerJob.StatusToCategory(inStatus))
            {
                case JobProgressCategory.Completed:
                case JobProgressCategory.Active:
                    {
                        m_ActionButton.gameObject.SetActive(false);
                        m_LockedGroup.gameObject.SetActive(false);
                        break;
                    }

                case JobProgressCategory.InProgress:
                    {
                        m_ActionButtonLabel.SetText(Label_ActivateJob);
                        m_ActionButton.gameObject.SetActive(true);
                        m_LockedGroup.gameObject.SetActive(false);
                        break;
                    }

                case JobProgressCategory.Available:
                    {
                        m_ActionButtonLabel.SetText(Label_AcceptJob);
                        m_ActionButton.gameObject.SetActive(true);
                        m_LockedGroup.gameObject.SetActive(false);
                        break;
                    }

                case JobProgressCategory.Locked:
                    {
                        m_ActionButton.gameObject.SetActive(false);
                        m_LockedGroup.gameObject.SetActive(true);
                        break;
                    }
            }
        }
        
        #if UNITY_EDITOR

        void IBakedComponent.Bake()
        {
            m_UpgradeDisplays = m_HasRequiredUpgradesGroup.gameObject.GetComponentsInChildren<RequiredUpgradeDisplay>(true);
        }

        #endif // UNITY_EDITOR
    }
}