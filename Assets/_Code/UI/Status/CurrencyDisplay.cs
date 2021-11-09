using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;

namespace Aqua {
    public class CurrencyDisplay : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField] private TMP_Text m_CoinsText = null;
        [SerializeField] private TMP_Text m_GearsText = null;

        #endregion // Inspector

        private void OnEnable() {
            Services.Events.Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged);
            if (!Services.State.IsLoadingScene()) {
                Refresh();
            }
        }

        private void OnDisable() {
            Services.Events?.Deregister<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged);
        }

        private void OnInventoryChanged(StringHash32 itemId) {
            if (itemId == ItemIds.Cash || itemId == ItemIds.Gear) {
                Refresh();
            }
        }

        private void Refresh() {
            var profile = Save.Inventory;
            m_CoinsText.text = profile.ItemCount(ItemIds.Cash).ToStringLookup();
            m_GearsText.text = profile.ItemCount(ItemIds.Gear).ToStringLookup();
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}