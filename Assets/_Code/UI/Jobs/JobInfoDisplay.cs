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
        [SerializeField] private LocText m_LocationLabel = null;
        [SerializeField] private LocText m_DescriptionLabel = null;
        [SerializeField] private LocText m_ShortDescriptionLabel = null;
        
        [Header("Extra Details")]
        [SerializeField, Required] private InvItemDisplay[] m_Rewards = null;
        [SerializeField] private RectTransform m_NoRewardsDisplay = null;
        [SerializeField, Required] private TickDisplay[] m_Difficulties = null;
        [SerializeField] private RectTransform m_CompletedDisplay = null;

        #endregion // Inspector

        public void Populate(JobDesc inJob, JobStatusFlags inStatus = JobStatusFlags.Mask_Available)
        {
            if (!inJob)
            {
                m_NameLabel.SetTextFromString(null);

                if (m_PosterLabel)
                    m_PosterLabel.SetTextFromString(null);

                if (m_LocationLabel)
                    m_LocationLabel.SetTextFromString(null);
                
                if (m_DescriptionLabel)
                    m_DescriptionLabel.SetTextFromString(null);

                if (m_ShortDescriptionLabel)
                    m_ShortDescriptionLabel.SetTextFromString(null);
                
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
                m_PosterLabel.SetText(Assets.Character(inJob.PosterId()).ShortNameId());

            if (m_LocationLabel)
            {
                var map = Assets.Map(inJob.StationId());
                m_LocationLabel.SetText(map.LabelId());
            }
            
            if (m_Rewards.Length > 0)
            {
                int rewardCount = 0;
                if (inJob.ExpReward() > 0)
                {
                    PopulateReward(rewardCount++, ItemIds.Exp, inJob.ExpReward());
                }
                if (inJob.CashReward() > 0)
                {
                    PopulateReward(rewardCount++, ItemIds.Cash, inJob.CashReward());
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

        public void UpdateStatus(JobDesc inJob, JobStatusFlags inStatus)
        {
            JobProgressCategory category = PlayerJob.StatusToCategory(inStatus);

            if (m_DescriptionLabel)
            {
                TextId desc = category == JobProgressCategory.Completed ? inJob.DescCompletedId() : inJob.DescId();
                m_DescriptionLabel.SetText(desc);
            }

            if (m_ShortDescriptionLabel)
                m_ShortDescriptionLabel.SetText(inJob.DescShortId());

            if (m_CompletedDisplay)
                m_CompletedDisplay.gameObject.SetActive(category == JobProgressCategory.Completed);
        }
    }
}