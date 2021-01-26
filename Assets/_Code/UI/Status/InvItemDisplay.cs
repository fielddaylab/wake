using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauUtil.Variants;
using BeauData;


namespace Aqua
{

    public class InvItemDisplay : MonoBehaviour
    {
        [SerializeField, Required] private Image Icon = null;

        [SerializeField, Required] private TextMeshProUGUI m_Text = null;

        [NonSerialized] private int m_Value = 0;

        [NonSerialized] private SerializedHash32 m_ItemId = null;

        [NonSerialized] private InvItem m_CurrentItem = null;

        public void SetupItem(InvItem item)
        {
            m_CurrentItem = item;
            Icon.sprite = item.Icon();
            m_Value = item.Value();
            m_ItemId = item.ItemId();

            m_Text.SetText(m_Value.ToString());

            return;
        }

        public void UpdateItem()
        {
            int value = Services.Data.Profile.Inventory.GetItem(m_ItemId).Value();
            if (m_Value != value)
            {
                m_Value = value;
            }
            m_Text.SetText(m_Value.ToString());
            return;

        }

    }
}