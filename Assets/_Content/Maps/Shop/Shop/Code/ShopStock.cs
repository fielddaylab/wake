using UnityEngine;
using BeauUtil;
using Aqua.Cameras;
using System.Collections;
using BeauRoutine;
using BeauUtil.UI;
using UnityEngine.EventSystems;
using System;
using Aqua.Scripting;
using Aqua.Profile;
using ScriptableBake;

namespace Aqua.Shop {
    public class ShopStock : MonoBehaviour, IBaked, ISceneLoadHandler {

        static public readonly StringHash32 Trigger_AttemptBuy = "ShopAttemptBuy";

        #region Inspector

        [SerializeField, HideInInspector] private ShopItem[] m_Items;
        [SerializeField] private Color m_CanAffordColor = Color.white;
        [SerializeField] private Color m_CannotAffordColor = Color.grey;

        #endregion // Inspector

        #region Callbacks

        private void OnEnable() {
            Services.Events.Register(GameEvents.InventoryUpdated, UpdateAllItems, this);
        }

        private void OnDisable() {
            Services.Events?.DeregisterAll(this);
        }

        private void OnItemClicked(PointerEventData eventData) {
            PointerListener.TryGetComponentUserData<ShopItem>(eventData, out ShopItem item);

            using(var table = TempVarTable.Alloc()) {
                table.Set("itemId", item.ItemId);
                table.Set("canAfford", CanAfford(Save.Inventory, item.CachedItem));
                table.Set("cashCost", item.CachedItem.CashCost());
                table.Set("expCost", item.CachedItem.RequiredExp());
                Services.Script.TriggerResponse(Trigger_AttemptBuy, table);
            }
        }

        static private bool CanAfford(InventoryData profile, InvItem item) {
            return profile.ItemCount(ItemIds.Cash) >= item.CashCost()
                && profile.ItemCount(ItemIds.Exp) >= item.RequiredExp();
        }

        #endregion // Callbacks

        #region Item Management

        public void SetItemsSelectable(bool selectable) {
            foreach(var item in m_Items) {
                item.Clickable.gameObject.SetActive(selectable);
                if (item.ItemDescriptionGroup) {
                    item.ItemDescriptionGroup.SetActive(selectable);
                }
            }
        }

        private void UpdateAllItems() {
            InventoryData profileData = Save.Inventory;

            foreach(var item in m_Items) {
                UpdateItem(item, profileData, true);
            }
        }

        private void UpdateItem(ShopItem item, InventoryData profile, bool showEffects) {
            InvItem itemData = item.CachedItem;
            bool bMissingPrerequisite = itemData.Prerequisite() != null && !profile.HasUpgrade(itemData.Prerequisite().Id());
            bool bSoldOut = (itemData.Category() == InvItemCategory.Upgrade && profile.HasUpgrade(item.ItemId))
                || itemData.HasFlags(InvItemFlags.OnlyOne) && profile.ItemCount(item.ItemId) > 0;

            if (bMissingPrerequisite) {
                item.AvailableRoot.SetActive(false);
                item.SoldOutRoot.SetActive(false);
                if (item.UnavailableRoot)
                    item.UnavailableRoot.SetActive(true);
            } else if (bSoldOut) {
                item.AvailableRoot.SetActive(false);
                item.SoldOutRoot.SetActive(true);
                if (item.UnavailableRoot)
                    item.UnavailableRoot.SetActive(false);
            } else {
                item.SoldOutRoot.SetActive(false);
                item.AvailableRoot.SetActive(true);
                if (item.UnavailableRoot)
                    item.UnavailableRoot.SetActive(false);

                if (itemData.CashCost() > 0) {
                    item.CoinsRoot.SetActive(true);
                    item.CoinsText.SetText(itemData.CashCost().ToStringLookup());
                    item.CoinsText.color = profile.ItemCount(ItemIds.Cash) >= itemData.CashCost() ? m_CanAffordColor : m_CannotAffordColor;
                } else {
                    item.CoinsRoot.SetActive(false);
                }

                if (itemData.RequiredExp() > 0) {
                    item.GearsRoot.SetActive(true);
                    item.GearsText.SetText(itemData.RequiredExp().ToStringLookup());
                    item.GearsText.color = profile.ItemCount(ItemIds.Exp) >= itemData.RequiredExp() ? m_CanAffordColor : m_CannotAffordColor;
                } else {
                    item.GearsRoot.SetActive(false);
                }

                item.ItemNameText.SetText(itemData.NameTextId());
            }
        }

        #endregion // Item Management

        #region Loading

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            InventoryData profileData = Save.Inventory;

            foreach(var item in m_Items) {
                item.CachedItem = Assets.Item(item.ItemId);
                item.Tooltip.TooltipId = item.CachedItem.NameTextId();
                item.Clickable.UserData = item;
                item.Clickable.onClick.AddListener(OnItemClicked);
                item.Clickable.gameObject.SetActive(false);
                if (item.ItemDescriptionGroup) {
                    item.ItemDescriptionGroup.gameObject.SetActive(false);
                }
                UpdateItem(item, profileData, false);
            }
        }

        #endregion // Loading

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            m_Items = FindObjectsOfType<ShopItem>();
            return true;
        }

        #endif // UNITY_EDITOR
    }
}