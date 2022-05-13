using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using UnityEngine.Serialization;

namespace Aqua {
    public class PlayerLevelDisplay : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField] private TMP_Text m_LevelText = null;
        [SerializeField] private TMP_Text m_ExpText = null;
        [SerializeField] private Image m_ExpBar = null;
        [SerializeField] private LocText m_ExpToNextLabel = null;
        [SerializeField] private TextId m_ExpToNextText = default;

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
            Services.Events.Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged, this)
                .Register(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated, this);
            if (!Script.IsLoading) {
                Refresh();
            }
        }

        private void OnDisable() {
            Services.Events?.Deregister<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged)
                .Deregister(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated);
        }

        private void Refresh() {
            if (m_LevelText) {
                m_LevelText.text = ((int) Save.ExpLevel + 1).ToStringLookup();
            }
            if (m_ExpText) {
                m_ExpText.text = ((int) Save.Exp).ToStringLookup();
            }
            if (m_ExpBar || m_ExpToNextLabel) {
                uint baseNext = ScienceUtils.ExpForLevel(Save.ExpLevel + 1);
                uint toNext = ScienceUtils.ExpForNextLevel(Save.Current);
                if (m_ExpBar) {
                    float percent = 1 - ((float) toNext / baseNext);
                    m_ExpBar.fillAmount = percent;
                }
                if (m_ExpToNextLabel) {
                    m_ExpToNextLabel.SetTextFromString(
                        Loc.Format(m_ExpToNextText, toNext)
                    );
                }
            }
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}