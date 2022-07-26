using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Cameras;
using Aqua.Scripting;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Aqua.Portable {
    public class TechApp : PortableMenuApp {
        #region Types

        public enum CategoryId {
            Exploration,
            Science,
            NONE = 255
        }

        #endregion //Types

        #region Inspector

        [SerializeField] private Color m_SelectedOutlineColor = Color.white;
        [SerializeField] private Color m_BaseOutlineColor = Color.white;

        [Header("Categories")]
        [SerializeField] private TechCategoryButton m_ExplorationCategory = null;
        [SerializeField] private TechCategoryButton m_ScienceCategory = null;

        [Header("Left Column")]
        [SerializeField] private LayoutGroup m_LeftColumnLayout = null;
        [SerializeField] private LocText m_LeftColumnHeader = null;
        [SerializeField] private PortableUpgradeButton[] m_LeftColumnButtons = null;

        [Header("Right Column")]
        [SerializeField] private LayoutGroup m_RightColumnLayout = null;
        [SerializeField] private LocText m_RightColumnHeader = null;
        [SerializeField] private PortableUpgradeButton[] m_RightColumnButtons = null;

        #endregion //Inspector

        [NonSerialized] private InvItem m_SelectedItem;
        [NonSerialized] private CategoryId m_CurrentCategory;

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            m_ExplorationCategory.Group.ForceActive(false);
            m_ScienceCategory.Group.ForceActive(false);

            m_ExplorationCategory.Toggle.onValueChanged.AddListener((_) => UpdateCategory(m_ExplorationCategory, CategoryId.Exploration, TechConsts.Trigger_OpenExploration));
            m_ScienceCategory.Toggle.onValueChanged.AddListener((_) => UpdateCategory(m_ScienceCategory, CategoryId.Science, TechConsts.Trigger_OpenScience));

            Services.Events.Register(GameEvents.InventoryUpdated, RefreshButtons, this);

            foreach(var button in m_LeftColumnButtons) {
                var cachedBtn = button;
                cachedBtn.Button.onClick.AddListener(() => OnButtonClicked(cachedBtn));
            }

            foreach(var button in m_RightColumnButtons) {
                var cachedBtn = button;
                cachedBtn.Button.onClick.AddListener(() => OnButtonClicked(cachedBtn));
            }
        }

        protected void OnEnable() { base.OnEnable(); }

        protected void OnDestroy() {
            Services.Events?.DeregisterAll(this);
            // base.OnDestroy();
        }

        #endregion //Unity Events

        #region BasePanel

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShow(inbInstant);
            RefreshButtons();
            // LoadData();
        }

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Page Display

        #endregion // Page Display

        #region Buttons

        private void OnButtonClicked(PortableUpgradeButton button) {
            button.Outline.Color = m_SelectedOutlineColor;
            m_SelectedItem = button.CachedItem;
            // Routine.Start(this, ButtonClickRoutine(button));
        }

        // private IEnumerator ButtonClickRoutine(PortableUpgradeButton button) {
        //     try {
        //         using(var table = TempVarTable.Alloc()) {
                    
        //             // bool canAfford = button.CachedStatus == ItemStatus.Available;

        //             table.Set("itemId", button.CachedItem.Id());
        //             // var thread = Services.Script.TriggerResponse(ShopConsts.Trigger_AttemptBuy, table);

        //             // if (!canAfford) {
        //             //     Services.Events.Dispatch(ShopConsts.Event_InsufficientFunds, button.CachedItem.Id());
        //             // }

        //             // m_Preview.ShowPreview(button.CachedItem);

        //             yield return ;

        //             // bool nowHasItem = Save.Inventory.HasUpgrade(button.CachedItem.Id());
        //             switch(m_CurrentCategory) {
        //                 case CategoryId.Exploration: {
        //                     Services.Script.TriggerResponse(TechConsts.Trigger_OpenExploration);
        //                     break;
        //                 }
        //                 case CategoryId.Science: {
        //                     Services.Script.TriggerResponse(TechConsts.Trigger_OpenScience);
        //                     break;
        //                 }
        //             }

        //         }
        //     } finally {
        //         m_SelectedItem = null;
        //         UpdateButtonState(button);
        //         // m_Preview.HidePreview();
        //     }
        // }

        #endregion // Buttons

        #region Categories

        private void UpdateCategory(TechCategoryButton category, CategoryId id, StringHash32 triggerId = default) {
            if (!IsShowing()) return;
    
            if (!category.Toggle.isOn) {
                category.Group.Deactivate();
                return;
            }

            m_CurrentCategory = id;

            if (category.Group.Activate()) {
                PopulateColumn(m_LeftColumnHeader, category.LeftHeader, m_LeftColumnButtons, category.LeftItems);
                PopulateColumn(m_RightColumnHeader, category.RightHeader, m_RightColumnButtons, category.RightItems);

                if (!triggerId.IsEmpty)
                    Services.Script.TriggerResponse(triggerId);
            }

            m_LeftColumnLayout.ForceRebuild();
            m_RightColumnLayout.ForceRebuild();
        }

        private void PopulateColumn(LocText header, TextId headerId, PortableUpgradeButton[] buttons, StringHash32[] itemIds) {
            header.SetText(headerId);

            PortableUpgradeButton button;
            InvItem item;
            for(int i = 0; i < itemIds.Length; i++) {
                item = Assets.Item(itemIds[i]);
                button = buttons[i];
                PopulateButton(button, item);
            }

            for(int i = itemIds.Length; i < buttons.Length; i++) {
                button = buttons[i];
                button.CachedItem = null;
                button.gameObject.SetActive(false);
            }
        }

        private void PopulateButton(PortableUpgradeButton button, InvItem item) {
            button.CachedItem = item;
            button.Title.SetText(item.NameTextId());
            button.Icon.sprite = item.Icon();
            button.Cursor.TooltipId = item.NameTextId();

            button.gameObject.SetActive(false);
            UpdateButtonState(button);
            button.gameObject.SetActive(true);
        }

        private void UpdateButtonState(PortableUpgradeButton button) {
            bool selected = m_SelectedItem == button.CachedItem;
            button.Outline.Color = m_BaseOutlineColor;
        }

        private void RefreshButtons() {
            foreach(var button in m_LeftColumnButtons) {
                if (button.isActiveAndEnabled) {
                    UpdateButtonState(button);
                }
            }

            foreach(var button in m_RightColumnButtons) {
                if (button.isActiveAndEnabled) {
                    UpdateButtonState(button);
                }
            }
        }

        #endregion // Categories
    }
}