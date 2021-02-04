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

        [NonSerialized] private string m_NameTextId = null;

        [NonSerialized] private PlayerInv m_CurrentItem = null;

        public void SetupItem(PlayerInv playerItem)
        {
            m_CurrentItem = playerItem;
            Icon.sprite = playerItem.Item.Icon();
            m_Value = playerItem.Value();
            m_NameTextId = playerItem.Item.NameTextId();

            m_Text.SetText(m_Value.ToString());

            return;
        }

    }
}