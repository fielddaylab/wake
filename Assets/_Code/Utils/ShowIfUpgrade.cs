using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class ShowIfUpgrade : MonoBehaviour
    {
        #region Inspector

        [SerializeField, ItemId(InvItemCategory.Upgrade)] private SerializedHash32 m_ItemName = null;
        [SerializeField] private GameObject[] m_ToShow = null;
        [SerializeField] private GameObject[] m_ToHide = null;
        [SerializeField, Tooltip("If set, this will be checked whenever the inventory is updated")] private bool m_ContinuousCheck = true;
        
        #endregion // Inspector

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private bool m_LastState;

        private void Awake()
        {
            if (m_ContinuousCheck)
            {
                Services.Events.Register(GameEvents.InventoryUpdated, Refresh, this);
            }

            if (Services.State.IsLoadingScene())
            {
                Services.Events.Register(GameEvents.SceneLoaded, Refresh, this);
            }
            else
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (!Services.Valid)
                return;

            Services.Events?.DeregisterAll(this);
        }

        private void Refresh()
        {
            SetState(Services.Data.Profile.Inventory.HasUpgrade(m_ItemName));
            if (!m_ContinuousCheck)
                Destroy(this);
        }

        private void SetState(bool inbState)
        {
            if (m_Initialized && inbState == m_LastState)
                return;

            m_Initialized = true;
            m_LastState = inbState;
            foreach(var obj in m_ToShow)
                obj.SetActive(inbState);
            foreach(var obj in m_ToHide)
                obj.SetActive(!inbState);
        }
    }
}