using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using UnityEngine.Serialization;

namespace Aqua {
    public class PlayerLevelIcon : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField, Required] private Image m_LevelIcon = null;
        [SerializeField] private Image m_Meter = null;

        #endregion // Inspector

        private readonly Action OnScienceLevelUpdated;
        private readonly Action<StringHash32> OnInventoryChanged;

        private PlayerLevelIcon() {
            OnScienceLevelUpdated = Refresh;
            OnInventoryChanged = (StringHash32 itemId) => {
                if (itemId == ItemIds.Exp) {
                    Refresh();
                }
            };
        }

        private void OnEnable() {
            Services.Events.Register(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated)
                .Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged);
            if (!Script.IsLoading) {
                Refresh();
            }
        }

        private void OnDisable() {
            Services.Events?.Deregister(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated)
                .Deregister<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged);
        }

        private void Refresh() {
            m_LevelIcon.sprite = Services.Tweaks.Get<ScienceTweaks>().LevelIcon((int) Save.ExpLevel);
            if (m_Meter != null) {
                float amt = ScienceUtils.EvaluateLevel(Save.Current).Percent;
                m_Meter.fillAmount = amt;
            }
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}