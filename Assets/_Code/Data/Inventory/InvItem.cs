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
        [SerializeField] private TextId m_NameTextId = default;
        [SerializeField] private TextId m_PluralNameTextId = default;
        [SerializeField] private TextId m_DescriptionTextId = default;

        [Header("Value")]
        [SerializeField, ShowIfField("IsCurrency")] private uint m_Default = 0;
        [SerializeField, ShowIfField("IsSellable")] private uint m_SellCoinsValue = 0;
        [SerializeField, ShowIfField("IsSellable")] private uint m_SellGearsValue = 0;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;

        [Header("Sorting")]
        [SerializeField, AutoEnum] private InvItemSubCategory m_SubCategory = InvItemSubCategory.None;
        [SerializeField] private int m_SortingOrder = 0;

        #endregion

        public InvItemCategory Category() { return m_Category; }
        public InvItemSubCategory SubCategory() { return m_SubCategory; }
        public InvItemFlags Flags() { return m_Flags; }

        public bool HasFlags(InvItemFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(InvItemFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public TextId NameTextId() { return m_NameTextId; }
        public TextId PluralNameTextId() { return m_PluralNameTextId.IsEmpty ? m_NameTextId : m_PluralNameTextId; }
        public TextId DescriptionTextId() { return m_DescriptionTextId; }
        
        public Sprite Icon() { return m_Icon; }

        public uint DefaultAmount() { return m_Default; }
        public uint SellCoinsValue() { return m_SellCoinsValue; }
        public uint SellGearsValue() { return m_SellGearsValue; }

        #region Sorting

        static public readonly Comparison<InvItem> SortByCategoryAndOrder = (x, y) =>
        {
            int categoryOrder = x.m_Category.CompareTo(y.m_Category);
            if (categoryOrder != 0)
                return categoryOrder;

            int subcategoryOrder = x.m_SubCategory.CompareTo(y.m_SubCategory);
            if (subcategoryOrder != 0)
                return subcategoryOrder;

            int orderOrder = x.m_SortingOrder.CompareTo(y.m_SortingOrder);
            if (orderOrder != 0)
                return orderOrder;

            return x.Id().CompareTo(y.Id());
        };

        #endregion // Sorting

        #if UNITY_EDITOR

        private bool IsCurrency()
        {
            return m_Category == InvItemCategory.Currency;
        }

        private bool IsSellable()
        {
            return (m_Flags & InvItemFlags.Sellable) != 0;
        }

        #endif // UNITY_EDITOR
    }

    public enum InvItemCategory : byte
    {
        Currency,
        Upgrade,
        Artifact
    }

    public enum InvItemSubCategory : byte
    {
        None,
        Ship,
        Submarine,
        Experimentation,
        Portable
    }

    [Flags]
    public enum InvItemFlags : byte
    {
        [Hidden]
        None = 0x00,

        Hidden = 0x01,
        Sellable = 0x02,
        AlwaysDisplay = 0x4,
    }

    public class ItemIdAttribute : DBObjectIdAttribute {
        public InvItemCategory? Category;

        public ItemIdAttribute() : base(typeof(InvItem)) {
            Category = null;
        }

        public ItemIdAttribute(InvItemCategory inCategory) : base(typeof(InvItem)) {
            Category = inCategory;
        }

        public override bool Filter(DBObject inObject) {
            if (Category.HasValue) {
                InvItem item = (InvItem) inObject;
                return item.Category() == Category.Value;
            } else {
                return true;
            }
        }
    }
}