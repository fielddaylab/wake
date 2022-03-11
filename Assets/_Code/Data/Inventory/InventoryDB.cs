using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using ScriptableBake;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Inventory Database", fileName = "InventoryDB")]
    public class InventoryDB : DBObjectCollection<InvItem>, IBaked
    {
        [SerializeField, HideInInspector] private int m_CurrencyCount;
        [SerializeField, HideInInspector] private int m_UpgradeCount;
        [SerializeField, HideInInspector] private int m_ArtifactCount;

        private readonly HashSet<StringHash32> m_CountableItemIds = new HashSet<StringHash32>();
        private readonly HashSet<StringHash32> m_AlwaysVisibleItemIds = new HashSet<StringHash32>();

        public ListSlice<InvItem> Currencies { get { return new ListSlice<InvItem>(m_Objects, 0, m_CurrencyCount); } }
        public ListSlice<InvItem> Upgrades { get { return new ListSlice<InvItem>(m_Objects, m_CurrencyCount, m_UpgradeCount); } }
        public ListSlice<InvItem> Artifacts { get { return new ListSlice<InvItem>(m_Objects, m_CurrencyCount + m_UpgradeCount, m_ArtifactCount); } }

        public bool IsCountable(StringHash32 inItemId)
        {
            return m_CountableItemIds.Contains(inItemId);
        }

        public bool IsAlwaysVisible(StringHash32 inItemId)
        {
            return m_AlwaysVisibleItemIds.Contains(inItemId);
        }

        protected override void ConstructLookupForItem(InvItem inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);

            switch(inItem.Category())
            {
                case InvItemCategory.Artifact:
                case InvItemCategory.Currency:
                    {
                        m_CountableItemIds.Add(inItem.Id());
                        break;
                    }
            }

            if (inItem.HasFlags(InvItemFlags.AlwaysDisplay))
            {
                m_AlwaysVisibleItemIds.Add(inItem.Id());
            }
        }

        #if UNITY_EDITOR

        static private readonly Comparison<InvItem> SortByCategory = (x, y) => {
            int categoryCompare = x.Category().CompareTo(y.Category());
            if (categoryCompare != 0)
                return categoryCompare;

            return x.Id().CompareTo(y.Id());
        };

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            SortObjects(SortByCategory);

            m_CurrencyCount = 0;
            m_UpgradeCount = 0;
            m_ArtifactCount = 0;

            foreach(var obj in m_Objects)
            {
                switch(obj.Category())
                {
                    case InvItemCategory.Currency:
                        m_CurrencyCount++;
                        break;

                    case InvItemCategory.Upgrade:
                        m_UpgradeCount++;
                        break;

                    case InvItemCategory.Artifact:
                        m_ArtifactCount++;
                        break;
                }
            }

            return true;
        }

        [UnityEditor.CustomEditor(typeof(InventoryDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}