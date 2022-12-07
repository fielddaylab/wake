using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using UnityEngine.Serialization;
using BeauRoutine;
using BeauPools;

namespace Aqua {
    public class PlayerLevelDisplay : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField] private TMP_Text m_ExpText = null;
        [SerializeField] private Image m_ExpBar = null;
        
        [Header("Exp Gain")]
        [SerializeField] private TMP_Text m_ExcessExpText = null;
        [SerializeField] private Image m_ExcessExpBar = null;

        [Header("Exp to Next")]
        [SerializeField] private LocText m_ExpToNextLabel = null;
        [SerializeField] private TextId m_ExpToNextText = default;
        
        [Header("Level Badge")]
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
            Populate(Save.Exp, 0);
        }

        public ScienceLevelProgress Populate(uint exp, uint excess) {
            var scienceTweaks = Services.Tweaks.Get<ScienceTweaks>();
            
            ScienceLevelProgress progress = ScienceUtils.EvaluateLevel(exp);

            if (progress.ToNext > 0) {
                m_ExpToNextLabel.Graphic.color = scienceTweaks.LevelColor((int) progress.Level);
                m_ExpToNextLabel.SetTextFromString(
                    Loc.Format(m_ExpToNextText, progress.ToNext.ToStringLookup(), (progress.Level + 1).ToStringLookup())
                );

                m_ExpText.SetText(progress.ExpForLevel.ToStringLookup());
                m_ExcessExpBar.gameObject.SetActive(false);

                float anchorPercent = progress.Percent;

                if (m_ExcessExpBar && m_ExcessExpText) {
                    if (excess > 0) {
                        anchorPercent = Math.Min(progress.Percent + (float) excess / ScienceUtils.ExpForLevel(progress.Level + 1), 1);
                        m_ExcessExpBar.fillAmount = anchorPercent;
                        m_ExcessExpBar.gameObject.SetActive(true);

                        using(var psb = PooledStringBuilder.Create()) {
                            psb.Builder.Append('+').AppendNoAlloc(excess);
                            m_ExcessExpText.SetText(psb);
                        }
                        m_ExcessExpText.ForceMeshUpdate(true, false);
                    } else {
                        m_ExcessExpBar.gameObject.SetActive(false);
                        m_ExcessExpText.gameObject.SetActive(false);
                    }
                }
                
                m_ExpText.ForceMeshUpdate(true, false);

                float maxWidth = m_ExpBar.rectTransform.rect.width;
                float barWidth = maxWidth * anchorPercent;
                float preferredWidth = m_ExpText.preferredWidth;

                float maxValue = barWidth;
                if (m_ExcessExpText && excess > 0) {
                    m_ExcessExpText.rectTransform.SetAnchorPos(preferredWidth, Axis.X);
                    maxValue = Math.Min(maxValue, maxWidth - m_ExcessExpBar.preferredWidth);
                }

                m_ExpText.rectTransform.SetAnchorPos(Math.Max(maxValue - preferredWidth - 8, 0), Axis.X);

            } else {
                if (m_ExcessExpBar) {
                    m_ExcessExpBar.gameObject.SetActive(false);
                }
                if (m_ExcessExpText) {
                    m_ExcessExpText.gameObject.SetActive(false);
                }

                m_ExpToNextLabel.gameObject.SetActive(false);
                m_ExpText.gameObject.SetActive(false);
            }

            m_ExpBar.fillAmount = progress.Percent;

            int desiredBadgeIndex = (int) progress.Level - m_BadgeDisplayOffset;
            if (progress.Level == scienceTweaks.MaxLevels()) {
                desiredBadgeIndex--;
            }

            for(int i = 0; i < m_BadgeDisplay.Length; i++) {
                m_BadgeDisplay[i].SetActive(i == desiredBadgeIndex);
            }

            return progress;
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}