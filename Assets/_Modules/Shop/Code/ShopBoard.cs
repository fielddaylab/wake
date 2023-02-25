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

namespace Aqua.Shop {
    public class ShopBoard : SharedPanel {

        #region Types

        public enum ItemStatus {
            Available,
            Locked,
            Purchased,
            CantAfford
        }

        public enum CategoryId {
            Exploration,
            Science,
            NONE = 255
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private CameraPose m_DefaultPose = null;

        [Header("Categories")]
        [SerializeField] private ShopCategoryButton m_ExplorationCategory = null;
        [SerializeField] private ShopCategoryButton m_ScienceCategory = null;

        [Header("Left Column")]
        [SerializeField] private LayoutGroup m_LeftColumnLayout = null;
        [SerializeField] private LocText m_LeftColumnHeader = null;
        [SerializeField] private ShopItemButton[] m_LeftColumnButtons = null;

        [Header("Right Column")]
        [SerializeField] private LayoutGroup m_RightColumnLayout = null;
        [SerializeField] private LocText m_RightColumnHeader = null;
        [SerializeField] private ShopItemButton[] m_RightColumnButtons = null;

        [Header("Button States")]
        [SerializeField] private Color m_LockedOutlineColor = Color.white;
        [SerializeField] private Color m_UnavailableOutlineColor = Color.white;
        [SerializeField] private Color m_AvailableOutlineColor = Color.white;
        [SerializeField] private Color m_SelectedOutlineColor = Color.white;
        [SerializeField] private Color m_PurchasedOutlineColor = Color.white;
        [SerializeField] private Sprite[] m_LevelRequirementIcons = null;

        [Header("Animation")]
        [SerializeField] private TweenSettings m_TweenOnAnim = new TweenSettings(0.2f);
        [SerializeField] private TweenSettings m_TweenOffAnim = new TweenSettings(0.2f);
        [SerializeField] private float m_OffscreenPos = 0;
        [SerializeField] private float m_OnscreenPos = 0;
        [SerializeField] private ShopPreview m_Preview = null;
        [SerializeField] private AppearAnimSet m_AppearSequence = null;

        #endregion

        [NonSerialized] private InvItem m_SelectedItem;
        [NonSerialized] private CategoryId m_CurrentCategory;

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            m_ExplorationCategory.Toggle.onValueChanged.AddListener((_) => UpdateCategory(m_ExplorationCategory, CategoryId.Exploration, ShopConsts.Trigger_OpenExploration));
            m_ScienceCategory.Toggle.onValueChanged.AddListener((_) => UpdateCategory(m_ScienceCategory, CategoryId.Science, ShopConsts.Trigger_OpenScience));

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

        protected override void Start() {
            base.Start();

            m_ExplorationCategory.Group.ForceActive(false);
            m_ScienceCategory.Group.ForceActive(false);
        }

        protected override void OnEnable() {
            base.OnEnable();
        }

        protected override void OnDestroy() {
            Services.Events?.DeregisterAll(this);
            base.OnDestroy();
        }

        #endregion // Unity Events

        #region Buttons

        private void OnButtonClicked(ShopItemButton button) {
            button.Outline.Color = m_SelectedOutlineColor;
            m_SelectedItem = button.CachedItem;
            Routine.Start(this, ButtonClickRoutine(button));
        }

        private IEnumerator ButtonClickRoutine(ShopItemButton button) {
            try {
                using(var table = TempVarTable.Alloc()) {
                    
                    bool canAfford = button.CachedStatus == ItemStatus.Available;

                    table.Set("itemId", button.CachedItem.Id());
                    table.Set("canAfford", canAfford);
                    table.Set("cashCost", button.CachedItem.CashCost());
                    table.Set("requiredLevel", button.CachedItem.RequiredLevel());
                    var thread = Services.Script.TriggerResponse(ShopConsts.Trigger_AttemptBuy, table);

                    if (!canAfford) {
                        Services.Events.Dispatch(ShopConsts.Event_InsufficientFunds, button.CachedItem.Id());
                    }

                    m_Preview.ShowPreview(button.CachedItem);

                    yield return thread.Wait();

                    // bool nowHasItem = Save.Inventory.HasUpgrade(button.CachedItem.Id());
                    switch(m_CurrentCategory) {
                        case CategoryId.Exploration: {
                            Services.Script.TriggerResponse(ShopConsts.Trigger_OpenExploration);
                            break;
                        }
                        case CategoryId.Science: {
                            Services.Script.TriggerResponse(ShopConsts.Trigger_OpenScience);
                            break;
                        }
                    }

                }
            } finally {
                m_SelectedItem = null;
                UpdateButtonState(button);
                m_Preview.HidePreview();
            }
        }

        #endregion // Buttons

        #region Categories

        private void UpdateCategory(ShopCategoryButton category, CategoryId id, StringHash32 triggerId = default, bool animate = true) {
            if (!IsShowing()) {
                return;
            }

            if (!category.Toggle.isOn) {
                category.Group.Deactivate();
                return;
            }

            m_CurrentCategory = id;

            if (category.Group.Activate()) {
                if (category.CameraPose) {
                    Services.Camera.MoveToPose(category.CameraPose, 0.5f, Curve.Smooth, Cameras.CameraPoseProperties.All);
                }

                PopulateColumn(m_LeftColumnHeader, category.LeftHeader, m_LeftColumnButtons, category.LeftItems);
                PopulateColumn(m_RightColumnHeader, category.RightHeader, m_RightColumnButtons, category.RightItems);

                if (animate) {
                    float delay = IsTransitioning() ? 0.28f : 0;
                    m_AppearSequence.Play(delay);
                }

                if (!triggerId.IsEmpty)
                    Services.Script.TriggerResponse(triggerId);
            }

            m_LeftColumnLayout.ForceRebuild();
            m_RightColumnLayout.ForceRebuild();

            m_Preview.SetCategory(id);
        }

        private void PopulateColumn(LocText header, TextId headerId, ShopItemButton[] buttons, StringHash32[] itemIds) {
            header.SetText(headerId);

            ShopItemButton button;
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

        private void PopulateButton(ShopItemButton button, InvItem item) {
            button.CachedItem = item;
            button.Title.SetText(item.NameTextId());
            button.Icon.sprite = item.Icon();
            button.Cursor.TooltipId = item.NameTextId();

            int cashCost = button.CachedItem.CashCost();
            int reqLevel = button.CachedItem.RequiredLevel();

            button.CashIcon.SetActive(cashCost > 0);
            button.CashCost.gameObject.SetActive(cashCost > 0);
            button.CashCost.SetTextFromString(cashCost.ToStringLookup());
            button.LevelRequirementIcon.sprite = m_LevelRequirementIcons[Math.Max(reqLevel - 1, 0)];

            button.gameObject.SetActive(false);
            UpdateButtonState(button);
            button.gameObject.SetActive(true);
        }

        private void UpdateButtonState(ShopItemButton button) {
            ItemStatus status = GetStatus(button.CachedItem);
            button.PurchasedRoot.SetActive(status == ItemStatus.Purchased);
            button.UnavailableRoot.SetActive(status == ItemStatus.Locked);

            bool interactable = status == ItemStatus.CantAfford || status == ItemStatus.Available;
            button.Button.interactable = interactable;
            button.CostRoot.SetActive(interactable);

            bool selected = m_SelectedItem == button.CachedItem;
            switch(status) {
                case ItemStatus.CantAfford: {
                    button.Outline.Color = selected ? m_SelectedOutlineColor : m_UnavailableOutlineColor;
                    break;
                }
                case ItemStatus.Available: {
                    button.Outline.Color = selected ? m_SelectedOutlineColor : m_AvailableOutlineColor;
                    break;
                }
                case ItemStatus.Locked: {
                    button.Outline.Color = m_LockedOutlineColor;
                    break;
                }
                case ItemStatus.Purchased: {
                    button.Outline.Color = m_PurchasedOutlineColor;
                    break;
                }
            }

            button.CachedStatus = status;
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

        static private ItemStatus GetStatus(InvItem item) {
            if (Save.Inventory.HasUpgrade(item.Id())) {
                return ItemStatus.Purchased;
            }
            if (item.Prerequisite() != null && !Save.Inventory.HasUpgrade(item.Prerequisite().Id())) {
                return ItemStatus.Locked;
            }
            if (Save.Cash < item.CashCost() || Save.ExpLevel < item.RequiredLevel()) {
                return ItemStatus.CantAfford;
            }
            return ItemStatus.Available;
        }

        #endregion // Categories

        #region Animations

        private IEnumerator PlayPurchasingEffects() {
            return m_Preview.AnimatePurchase();
        }

        #endregion // Animations

        #region BasePanel

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_ExplorationCategory.Toggle.SetIsOnWithoutNotify(true);
            m_ScienceCategory.Toggle.SetIsOnWithoutNotify(false);
            Async.InvokeAsync(() => {
                UpdateCategory(m_ExplorationCategory, CategoryId.Exploration);
                UpdateCategory(m_ScienceCategory, CategoryId.Science);

                Services.Script.TriggerResponse(ShopConsts.Trigger_OpenMenu);
            });
        }

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            if (!inbInstant) {
                Services.Camera.MoveToPose(m_DefaultPose, 0.5f, Curve.Smooth, CameraPoseProperties.All);
                Services.Script.TriggerResponse(ShopConsts.Trigger_Close);
            }

            m_Preview.ClearCategory();
        }

        protected override void OnHideComplete(bool _) {
            base.OnHideComplete(_);
        }

        protected override void InstantTransitionToHide() {
            Root.SetAnchorPos(m_OffscreenPos, Axis.Y);
            CanvasGroup.Hide();
        }

        protected override void InstantTransitionToShow() {
            Root.SetAnchorPos(m_OnscreenPos, Axis.Y);
            CanvasGroup.Show();
        }

        protected override IEnumerator TransitionToHide() {
            yield return Root.AnchorPosTo(m_OffscreenPos, m_TweenOffAnim, Axis.Y);
            CanvasGroup.Hide();
        }

        protected override IEnumerator TransitionToShow() {
            CanvasGroup.Show();
            yield return Root.AnchorPosTo(m_OnscreenPos, m_TweenOnAnim, Axis.Y);
        }

        #endregion // BasePanel

        [LeafMember("ShopPlayPurchaseAnimation"), Preserve]
        static private IEnumerator LeafPlayPurchaseAnimation() {
            var ctrl = Services.UI.FindPanel<ShopBoard>();
            Assert.NotNull(ctrl);
            return ctrl.PlayPurchasingEffects();
        }
    }
}