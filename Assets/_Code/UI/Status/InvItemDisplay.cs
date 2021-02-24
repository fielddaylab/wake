using System;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;

namespace Aqua
{
    public class InvItemDisplay : MonoBehaviour
    {
        [SerializeField, Required] private Image Icon = null;
        [SerializeField, Required] private TMP_Text m_Text = null;

        [NonSerialized] private InvItem m_Item = null;
        [NonSerialized] private int m_Value = 0;

        public void SetupItem(PlayerInv playerItem)
        {
            SetupItem(playerItem.Item, playerItem.Value());
        }

        public void SetupItem(StringHash32 inItemId, int inValue)
        {
            SetupItem(Services.Assets.Inventory.Get(inItemId), inValue);
        }

        public void SetupItem(InvItem inItem, int inValue)
        {
            m_Item = inItem;
            m_Value = inValue;

            Icon.sprite = inItem.Icon();
            m_Text.SetText(m_Value.ToStringLookup());
        }

    }
}