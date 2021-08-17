using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Inventory/Inventory Item", fileName = "NewInvItem")]
    public class InvItem : DBObject
    {
        #region Inspector

        [SerializeField, AutoEnum] private InvItemCategory m_Category = InvItemCategory.Currency;
        [SerializeField, AutoEnum] private InvItemFlags m_Flags = InvItemFlags.None;

        [Header("Text")]
        [SerializeField] private TextId m_NameTextId = null;
        [SerializeField] private TextId m_DescriptionTextId = null;

        [Header("Value")]
        [SerializeField, Range(0, 2000)] private int m_Default = 0;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;

        #endregion

        public InvItemCategory Category() { return m_Category; }
        public InvItemFlags Flags() { return m_Flags; }

        public bool HasFlags(InvItemFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(InvItemFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public TextId NameTextId() { return m_NameTextId; }
        public TextId DescriptionTextId() { return m_DescriptionTextId; }
        
        public Sprite Icon() { return m_Icon; }

        public int DefaultValue() { return m_Default; }
    }

    public enum InvItemCategory : byte
    {
        Currency,
        Upgrade,
    }

    [Flags]
    public enum InvItemFlags : byte
    {
        [Hidden]
        None = 0x00,

        Hidden = 0x01
    }
}