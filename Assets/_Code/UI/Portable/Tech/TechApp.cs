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
        [SerializeField] private AppearAnimSet m_CategoryApparAnim = null;

        [Header("Left Column")]
        [SerializeField] private LayoutGroup m_LeftColumnLayout = null;
        [SerializeField] private LocText m_LeftColumnHeader = null;
        [SerializeField] private PortableUpgradeButton[] m_LeftColumnButtons = null;

        [Header("Right Column")]
        [SerializeField] private LayoutGroup m_RightColumnLayout = null;
        [SerializeField] private LocText m_RightColumnHeader = null;
        [SerializeField] private PortableUpgradeButton[] m_RightColumnButtons = null;

        [Header("Item Description")]
        [SerializeField] private GameObject m_ItemDescriptionLayout = null;
        [SerializeField] private LocText m_ItemDescription = null;
        [SerializeField] private Image m_LargeItemIcon = null;

        #endregion //Inspector

        [NonSerialized] private InvItem m_SelectedItem;
        [NonSerialized] private CategoryId m_CurrentCategory;

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            m_ExplorationCategory.Group.ForceActive(false);
            m_ScienceCategory.Group.ForceActive(false);

            m_ExplorationCategory.Toggle.onValueChanged.AddListener((_) => UpdateCategory(m_ExplorationCategory, CategoryId.Exploration));
            m_ScienceCategory.Toggle.onValueChanged.AddListener((_) => UpdateCategory(m_ScienceCategory, CategoryId.Science));

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

        protected override void OnEnable() {
            base.OnEnable(); 
            ClearAll();
        }

        protected void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        #endregion //Unity Events

        #region BasePanel

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShow(inbInstant);
        }

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);
            ClearAll();
        }

        #endregion // Panel

        #region Page Display

        private void ClearAll(){
            m_ScienceCategory.Toggle.SetIsOnWithoutNotify(false);
            m_ExplorationCategory.Toggle.SetIsOnWithoutNotify(true);

            m_CurrentCategory = CategoryId.Exploration;
            UpdateCategory(m_ExplorationCategory, CategoryId.Exploration, false);
            UpdateCategory(m_ScienceCategory, CategoryId.Exploration, false);

            m_SelectedItem = null;
            RefreshButtons();

            m_ItemDescriptionLayout.SetActive(false);
        }

        #endregion // Page Display

        #region Buttons

        private void OnButtonClicked(PortableUpgradeButton button) {
            button.Outline.Color = m_SelectedOutlineColor;
            m_SelectedItem = button.CachedItem;
            // Routine.Start(this, ButtonClickRoutine(button));
            RefreshButtons();
        }

        #endregion // Buttons

        #region Categories

        private void UpdateCategory(TechCategoryButton category, CategoryId id, bool animate = true) {
            if (!IsShowing()) return;
    
            if (!category.Toggle.isOn) {
                category.Group.Deactivate();
                return;
            }

            m_CurrentCategory = id;

            if (category.Group.Activate()) {
                PopulateColumn(m_LeftColumnHeader, category.LeftHeader, m_LeftColumnButtons, category.LeftItems);
                PopulateColumn(m_RightColumnHeader, category.RightHeader, m_RightColumnButtons, category.RightItems);

                m_LeftColumnLayout.ForceRebuild();
                m_RightColumnLayout.ForceRebuild();
                m_ItemDescriptionLayout.SetActive(false);

                if (animate) {
                    m_CategoryApparAnim.Play();
                }
            }
        }

        private void PopulateColumn(LocText header, TextId headerId, PortableUpgradeButton[] buttons, StringHash32[] itemIds) {
            header.SetText(headerId);

            PortableUpgradeButton button;
            InvItem item;
            for(int i = 0; i < itemIds.Length; i++) {
                // if(Save.Inventory.HasItem(itemIds[i])) {
                if(Save.Inventory.HasUpgrade(itemIds[i])){
                    item = Assets.Item(itemIds[i]);
                    button = buttons[i];
                    PopulateButton(button, item);
                } else {
                    button = buttons[i];
                    button.CachedItem = null;
                    button.gameObject.SetActive(false);
                }
            }

            for(int i = itemIds.Length; i < buttons.Length; i++) {
                button = buttons[i];
                button.CachedItem = null;
                button.gameObject.SetActive(false);
            }
        }

        // called from collumn to fill button info 
        private void PopulateButton(PortableUpgradeButton button, InvItem item) {
            button.CachedItem = item;
            button.Title.SetText(item.NameTextId());
            button.Icon.sprite = item.Icon();
            button.Cursor.TooltipId = item.NameTextId();
            button.Outline.Color = m_BaseOutlineColor;
            button.gameObject.SetActive(true);

            UpdateButtonState(button);
        }

        // updates the info section of the tab to display item info
        private void UpdateInfoSection(PortableUpgradeButton selected) {
            m_ItemDescriptionLayout.SetActive(true);
            m_ItemDescriptionLayout.GetComponent<AppearAnimSet>().Play();
            m_ItemDescription.SetText(selected.CachedItem.DescriptionTextId());
            m_LargeItemIcon.sprite = selected.Icon.sprite;
        }

        // called to update button outlines based on button states
        private void RefreshButtons() {
            foreach(var button in m_LeftColumnButtons) {
                if (button.isActiveAndEnabled) UpdateButtonState(button);
            }
            foreach(var button in m_RightColumnButtons) {
                if (button.isActiveAndEnabled) UpdateButtonState(button);
            }
        }

        private void UpdateButtonState(PortableUpgradeButton button) {
            // button.gameObject.SetActive(false);

            bool selected = m_SelectedItem == button.CachedItem;

            if(!selected) { button.Outline.Color = m_BaseOutlineColor; }
            else { UpdateInfoSection(button); }

            // button.gameObject.SetActive(true);
        }

        #endregion // Categories
    }
}