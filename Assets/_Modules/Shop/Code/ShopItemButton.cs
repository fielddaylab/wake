using System;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Shop {
    public class ShopItemButton : MonoBehaviour {
        public Button Button;
        public ColorGroup Outline;
        public PointerListener Listener;

        [Header("Info")]
        public LocText Title;
        public Image Icon;
        public CursorInteractionHint Cursor;

        [Header("Cost")]
        public GameObject LevelRequirementIcon;
        public LocText LevelRequirementObject;
        public LocText CashCost;
        public GameObject CashIcon;
        public GameObject CostDivider;

        [Header("States")]
        public GameObject UnavailableRoot;
        public GameObject PurchasedRoot;
        public GameObject CostRoot;

        [NonSerialized] public InvItem CachedItem;
        [NonSerialized] public ShopBoard.ItemStatus CachedStatus;
    }
}