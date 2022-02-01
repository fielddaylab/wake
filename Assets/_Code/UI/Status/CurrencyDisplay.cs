using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using UnityEngine.Serialization;

namespace Aqua {
    public class CurrencyDisplay : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField] private TMP_Text m_CoinsText = null;

        #endregion // Inspector

        private void OnEnable() {
            Services.Events.Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged);
            if (!Script.IsLoading) {
                Refresh();
            }
        }

        private void OnDisable() {
            Services.Events?.Deregister<StringHash32>(GameEvents.InventoryUpdated, OnInventoryChanged);
        }

        private void OnInventoryChanged(StringHash32 itemId) {
            if (itemId == ItemIds.Cash) {
                Refresh();
            }
        }

        private void Refresh() {
            m_CoinsText.text = ((int) Save.Cash).ToStringLookup();
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}