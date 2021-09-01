using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System;

namespace Aqua
{
    public class JobInfoDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private LocText m_NameLabel = null;
        [SerializeField] private LocText m_PosterLabel = null;
        [SerializeField] private LocText m_DescriptionLabel = null;
        [SerializeField] private Image m_Icon = null;
        
        [SerializeField, Required] private InvItemDisplay[] m_Rewards = null;
        [SerializeField] private RectTransform m_NoRewardsDisplay = null;
        [SerializeField, Required] private TickDisplay[] m_Difficulties = null;
        [SerializeField] private RectTransform m_CompletedDisplay = null;

        #endregion // Inspector

        public void Populate(JobDesc inJob, PlayerJobStatus inStatus = PlayerJobStatus.NotStarted)
        {
            if (!inJob)
            {
                m_NameLabel.SetText(null);

                if (m_PosterLabel)
                    m_PosterLabel.SetText(null);
                
                if (m_DescriptionLabel)
                    m_DescriptionLabel.SetText(null);

                if (m_Icon)
                    m_Icon.gameObject.SetActive(false);
                
                for(int i = 0; i < m_Rewards.Length; ++i)
                    m_Rewards[i].gameObject.SetActive(false);
                for(int i = 0; i < m_Difficulties.Length; ++i)
                    m_Difficulties[i].gameObject.SetActive(false);

                if (m_NoRewardsDisplay)
                    m_NoRewardsDisplay.gameObject.SetActive(false);

                if (m_CompletedDisplay)
                    m_CompletedDisplay.gameObject.SetActive(false);

                return;
            }

            m_NameLabel.SetText(inJob.NameId());
            
            if (m_PosterLabel)
                m_PosterLabel.SetText(inJob.PosterId());

            if (m_Icon)
            {
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
            
            if (m_Rewards.Length > 0)
            {
                int rewardCount = 0;
                if (inJob.CashReward() > 0)
                {
                    PopulateReward(rewardCount++, ItemIds.Cash, inJob.CashReward());
                }
                if (inJob.GearReward() > 0)
                {
                    PopulateReward(rewardCount++, ItemIds.Cash, inJob.GearReward());
                }

                for(int i = rewardCount; i < m_Rewards.Length; ++i)
                {
                    m_Rewards[i].gameObject.SetActive(false);
                }

                if (m_NoRewardsDisplay)
                    m_NoRewardsDisplay.gameObject.SetActive(rewardCount == 0);
            }

            if (m_Difficulties.Length > 0)
            {
                for(int i = 0; i < m_Difficulties.Length; ++i)
                {
                    int difficulty = inJob.Difficulty((ScienceActivityType) i);
                    m_Difficulties[i].Display(difficulty);
                }
            }

            UpdateStatus(inJob, inStatus);
        }

        private void PopulateReward(int inIndex, StringHash32 inId, int inAmount)
        {
            InvItemDisplay item = m_Rewards[inIndex];
            item.gameObject.SetActive(true);
            item.Populate(inId, inAmount);
        }

        public void UpdateStatus(JobDesc inJob, PlayerJobStatus inStatus)
        {
            if (m_DescriptionLabel)
            {
                StringHash32 desc = inJob.DescCompletedId();
                if (desc.IsEmpty || inStatus != PlayerJobStatus.Completed)
                    desc = inJob.DescId();
                m_DescriptionLabel.SetText(desc);
            }

            if (m_CompletedDisplay)
                m_CompletedDisplay.gameObject.SetActive(inStatus == PlayerJobStatus.Completed);
        }
    }
}