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
        public AppearAnim Anim;

        [Header("Info")]
        public LocText Title;
        public Image Icon;
        public CursorInteractionHint Cursor;

        [Header("Cost")]
        public Image LevelRequirementIcon;
        public LocText CashCost;
        public GameObject CashIcon;

        [Header("States")]
        public GameObject UnavailableRoot;
        public GameObject PurchasedRoot;
        public GameObject CostRoot;

        [NonSerialized] public InvItem CachedItem;
        [NonSerialized] public ShopBoard.ItemStatus CachedStatus;
    }
}