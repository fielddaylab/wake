using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua.Profile;

namespace Aqua.Portable
{
    public class TechApp : PortableMenuApp
    {
        #region Inspector

        [Header("Tech")]

        [SerializeField, Required] private PortableUpgradeSection m_TechSubmarineSection = null;
        [SerializeField, Required] private PortableUpgradeSection m_TechExperimentSection = null;
        [SerializeField, Required] private PortableUpgradeSection m_TechTabletSection = null;

        #endregion

        #region Panel

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShow(inbInstant);
            LoadData();
        }

        protected override void OnHide(bool inbInstant)
        {
            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Page Display

        private void LoadData()
        {
            using(PooledList<InvItem> upgrades = PooledList<InvItem>.Create())
            {
                foreach(var upgrade in Save.Inventory.GetItems(InvItemCategory.Upgrade))
                {
                    upgrades.Add(Assets.Item(upgrade.ItemId));
                }

                upgrades.Sort(InvItem.SortByCategoryAndOrder);

                m_TechSubmarineSection.Clear();
                m_TechExperimentSection.Clear();
                m_TechTabletSection.Clear();

                InvItemSubCategory currentCategory = InvItemSubCategory.None;
                int startIdx = 0;
                InvItem currentItem;

                for(int i = 0; i < upgrades.Count; i++)
                {
                    currentItem = upgrades[i];
                    if (currentItem.SubCategory() != currentCategory)
                    {
                        PopulateTechCategory(currentCategory, new ListSlice<InvItem>(upgrades, startIdx, i - startIdx));
                        startIdx = i;
                        currentCategory = currentItem.SubCategory();
                    }
                }

                PopulateTechCategory(currentCategory, new ListSlice<InvItem>(upgrades, startIdx, upgrades.Count - startIdx));
            }
        }

        private void PopulateTechCategory(InvItemSubCategory inCategory, ListSlice<InvItem> inItems)
        {
            switch(inCategory)
            {
                case InvItemSubCategory.Experimentation:
                    m_TechExperimentSection.Load(inItems);
                    break;

                case InvItemSubCategory.Portable:
                    m_TechTabletSection.Load(inItems);
                    break;

                case InvItemSubCategory.Submarine:
                    m_TechSubmarineSection.Load(inItems);
                    break;
            }
        }

        #endregion // Page Display
    }
}