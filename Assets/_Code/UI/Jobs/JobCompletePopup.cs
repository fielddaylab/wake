using System;
using System.Collections;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class JobCompletePopup : MonoBehaviour
    {
        #region Inspector

        [Header("Job Giver Portrait")]
        [SerializeField] private Image m_JobGiverPortrait = null;
        [SerializeField] private AppearAnim m_JobGiverPortraitAnim = null;

        [Header("Quote")]
        [SerializeField] private LocText m_JobGiverQuote = null;
        [SerializeField] private AppearAnim m_JobGiverQuoteAnim = null;

        [Header("Earnings")]
        [SerializeField] private AppearAnim m_EarnedAnim = null;
        [SerializeField] private TMP_Text m_EarnCoinCount = null;
        [SerializeField] private AppearAnim m_EarnCoinAnim = null;

        [Header("Experience")]
        [SerializeField] private PlayerLevelDisplay m_LevelDisplay = null;
        [SerializeField] private AppearAnim m_LevelDisplayAnim = null;

        #endregion // Inspector

        [NonSerialized] private JobDesc m_Job;
        [NonSerialized] private uint m_OldExp;
        [NonSerialized] private uint m_OldLevel;

        private void OnDisable() {
            m_Job = null;
        }

        public void Prepare(JobDesc job, uint oldExp) {
            m_JobGiverPortrait.sprite = Assets.Character(job.PosterId()).DefaultPortrait();
            m_JobGiverPortraitAnim.Hide();

            m_JobGiverQuoteAnim.Hide();
            // todo: randomize quotes?

            m_EarnedAnim.Hide();
            m_EarnCoinCount.SetText("+" + job.CashReward().ToStringLookup());
            m_EarnCoinAnim.Hide();

            m_LevelDisplayAnim.Hide();
            m_OldExp = oldExp;
            var displayedRank = m_LevelDisplay.Populate(m_OldExp, (uint) job.ExpReward());
            
            m_OldLevel = displayedRank.Level;
            m_Job = job;
        }

        public IEnumerator Execute(PopupPanel panel, PopupLayout layout) {
            m_JobGiverPortraitAnim.Ping(0.1f);
            m_JobGiverQuoteAnim.Ping(0.5f);
            yield return 2;
            m_EarnedAnim.Ping();
            yield return 0.5f;
            m_EarnCoinAnim.Ping();
            Services.Audio.PostEvent("job.reward");
            yield return 1;

            m_LevelDisplayAnim.Ping();
            yield return 0.5f;

            uint currentExp = Save.Exp;
            while(m_OldExp < currentExp) {
                m_OldExp = Math.Min(m_OldExp + 1, currentExp);
                uint displayedRank = m_LevelDisplay.Populate(m_OldExp, currentExp - m_OldExp).Level;
                if (displayedRank != m_OldLevel) {
                    m_OldLevel = displayedRank;
                    layout.Flash();
                    Services.Audio.PostEvent("Popup.LevelUpPreview");
                    // TODO: Play level up sound
                    yield return 0.2f;
                } else {
                    yield return 0.05f;
                }
            }
            yield return 0.5f;
        }
    }
}