using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using UnityEngine.Serialization;
using BeauRoutine;

namespace Aqua {
    public class PlayerLevelDisplay : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField] private TMP_Text m_ExpText = null;
        [SerializeField] private Image m_ExpBar = null;
        [SerializeField] private LocText m_ExpToNextLabel = null;
        [SerializeField] private TextId m_ExpToNextText = default;
        [SerializeField] private GameObject[] m_BadgeDisplay = null;
        [SerializeField] private int m_BadgeDisplayOffset = 1;

        #endregion // Inspector

        private readonly Action<StringHash32> OnInventoryChanged;
        private readonly Action OnScienceLevelUpdated;

        private PlayerLevelDisplay() {
            OnInventoryChanged = (StringHash32 itemId) => {
                if (itemId == ItemIds.Exp) {
                    Refresh();
                }
            };

            OnScienceLevelUpdated = Refresh;
        }

        private void OnEnable() {
            Services.Events.Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged)
                .Register(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated);
            if (!Script.IsLoading) {
                Refresh();
            }
        }

        private void OnDisable() {
            Services.Events?.Deregister<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged)
                .Deregister(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated);
        }

        private void Refresh() {
            var scienceTweaks = Services.Tweaks.Get<ScienceTweaks>();
            m_ExpText.text = Save.Exp.ToStringLookup();
            ScienceLevelProgress progress = ScienceUtils.EvaluateLevel(Save.Current);
            if (progress.ToNext > 0) {
                m_ExpToNextLabel.Graphic.color = scienceTweaks.LevelColor((int) Save.ExpLevel + 1);
                m_ExpToNextLabel.SetTextFromString(
                    Loc.Format(m_ExpToNextText, progress.ToNext.ToStringLookup(), (Save.ExpLevel + 1).ToStringLookup())
                );

                m_ExpText.SetText(progress.ExpForLevel.ToStringLookup());
                m_ExpText.ForceMeshUpdate(true, false);
                
                float totalWidth = m_ExpBar.rectTransform.rect.width * progress.Percent;
                float preferredWidth = m_ExpText.preferredWidth;

                m_ExpText.rectTransform.SetAnchorPos(Math.Max(totalWidth - preferredWidth - 8, 0), Axis.X);
            } else {
                m_ExpToNextLabel.gameObject.SetActive(false);
                m_ExpText.gameObject.SetActive(false);
            }

            m_ExpBar.fillAmount = progress.Percent;

            int desiredBadgeIndex = (int) Save.ExpLevel - m_BadgeDisplayOffset;
            if (Save.ExpLevel == scienceTweaks.MaxLevels()) {
                desiredBadgeIndex--;
            }

            for(int i = 0; i < m_BadgeDisplay.Length; i++) {
                m_BadgeDisplay[i].SetActive(i == desiredBadgeIndex);
            }
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}