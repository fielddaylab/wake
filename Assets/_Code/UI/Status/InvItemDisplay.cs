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
        [SerializeField] private string m_ValuePrefix = string.Empty;
        [SerializeField] private bool m_MatchColor = false;

        [NonSerialized] private InvItem m_Item = null;
        [NonSerialized] private int m_Value = 0;
        [NonSerialized] private Color m_OriginalColor = default;

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
            m_Text.SetText(m_ValuePrefix + m_Value.ToStringLookup());

            if (m_NameText)
                m_NameText.SetText(inItem.NameTextId());

            if (m_MatchColor)
            {
                if (m_OriginalColor.a == 0)
                    m_OriginalColor = m_Text.color;

                Color color = AQColors.ForItem(inItem.Id(), m_OriginalColor);
                m_Text.color = color;
                if (m_NameText)
                    m_NameText.Graphic.color = color;
            }
        }

    }
}