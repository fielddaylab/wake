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
        [SerializeField] private LocText m_NameText = null;

        [NonSerialized] private InvItem m_Item = null;
        [NonSerialized] private int m_Value = 0;

        public void Populate(PlayerInv playerItem)
        {
            Populate(playerItem.ItemId, (int) playerItem.Count);
        }

        public void Populate(StringHash32 inItemId, int inValue)
        {
            Populate(Assets.Item(inItemId), inValue);
        }

        public void Populate(InvItem inItem, int inValue)
        {
            m_Item = inItem;
            m_Value = inValue;

            Icon.sprite = inItem.Icon();
            m_Text.SetText(m_Value.ToStringLookup());

            if (m_NameText)
                m_NameText.SetText(inItem.NameTextId());
        }

    }
}