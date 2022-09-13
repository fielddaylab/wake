using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class ShowOnUpgrade : MonoBehaviour
    {
        #region Inspector

        [SerializeField, ItemId(InvItemCategory.Upgrade)] private SerializedHash32 m_ItemName = null;
        [SerializeField] private GameObject[] m_ToShow = null;
        [SerializeField] private GameObject[] m_ToHide = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private bool m_LastState;

        private void Awake() {
            Services.Events.Register<StringHash32>(GameEvents.InventoryUpdated, Refresh, this);
        }

        private void OnDestroy() {
            if (!Services.Valid)
                return;

            Services.Events?.DeregisterAll(this);
        }

        private void Refresh(StringHash32 inItemId) {
            SetState(inItemId == m_ItemName);
        }

        private void SetState(bool inbState) {
            if (m_Initialized && inbState == m_LastState)
                return;

            m_Initialized = true;
            m_LastState = inbState;
            foreach (var obj in m_ToShow)
                obj.SetActive(inbState);
            foreach (var obj in m_ToHide)
                obj.SetActive(!inbState);
        }
    }
}