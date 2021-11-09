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

namespace Aqua.Shop {
    public class ShopStock : MonoBehaviour, ISceneOptimizable, ISceneLoadHandler {

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
                table.Set("itemName", item.CachedItem.NameTextId().Hash());
                table.Set("canAfford", CanAfford(Save.Inventory, item.CachedItem));
                table.Set("cashCost", item.CachedItem.BuyCoinsValue());
                table.Set("gearCost", item.CachedItem.BuyGearsValue());
                Services.Script.TriggerResponse(Trigger_AttemptBuy, table);
            }
        }

        static private bool CanAfford(InventoryData profile, InvItem item) {
            return profile.ItemCount(ItemIds.Cash) >= item.BuyCoinsValue()
                && profile.ItemCount(ItemIds.Gear) >= item.BuyGearsValue();
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
                UpdateItem(item, profileData);
            }
        }

        private void UpdateItem(ShopItem item, InventoryData profile) {
            InvItem itemData = item.CachedItem;
            bool bSoldOut = (itemData.Category() == InvItemCategory.Upgrade && profile.HasUpgrade(item.ItemId))
                || itemData.HasFlags(InvItemFlags.OnlyOne) && profile.ItemCount(item.ItemId) > 0;

            if (bSoldOut) {
                item.AvailableRoot.SetActive(false);
                item.SoldOutRoot.SetActive(true);
            } else {
                item.SoldOutRoot.SetActive(false);
                item.AvailableRoot.SetActive(true);

                if (itemData.BuyCoinsValue() > 0) {
                    item.CoinsRoot.SetActive(true);
                    item.CoinsText.SetText(itemData.BuyCoinsValue().ToStringLookup());
                    item.CoinsText.color = profile.ItemCount(ItemIds.Cash) >= itemData.BuyCoinsValue() ? m_CanAffordColor : m_CannotAffordColor;
                } else {
                    item.CoinsRoot.SetActive(false);
                }

                if (itemData.BuyGearsValue() > 0) {
                    item.GearsRoot.SetActive(true);
                    item.GearsText.SetText(itemData.BuyGearsValue().ToStringLookup());
                    item.GearsText.color = profile.ItemCount(ItemIds.Gear) >= itemData.BuyGearsValue() ? m_CanAffordColor : m_CannotAffordColor;
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
                UpdateItem(item, profileData);
            }
        }

        #endregion // Loading

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize() {
            m_Items = FindObjectsOfType<ShopItem>();
        }

        #endif // UNITY_EDITOR
    }
}