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

        [SerializeField, Required] private PortableUpgradeSection m_GlobalSection = null;

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
                    if (upgrade.Item.HasFlags(InvItemFlags.Hidden))
                        continue;
                    upgrades.Add(upgrade.Item);
                }

                upgrades.Sort(InvItem.SortByCategoryAndOrder);

                m_GlobalSection.Clear();
                m_GlobalSection.Load(upgrades);
            }
        }

        #endregion // Page Display
    }
}