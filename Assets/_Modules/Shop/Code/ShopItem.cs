using UnityEngine;
using BeauUtil;
using BeauUtil.UI;
using System;
using TMPro;

namespace Aqua.Shop
{
    public class ShopItem : MonoBehaviour
    {
        [ItemId] public SerializedHash32 ItemId = default;

        [Header("Active")]
        public GameObject AvailableRoot;
        public PointerListener Clickable;
        public CursorInteractionHint Tooltip;
        public GameObject ItemDescriptionGroup;
        public LocText ItemNameText;
        
        [Header("Cost")]
        public GameObject CoinsRoot;
        public TMP_Text CoinsText;
        public GameObject GearsRoot;
        public TMP_Text GearsText;
        
        [Header("Sold Out")]
        public GameObject SoldOutRoot;

        [Header("Cannot Access")]
        public GameObject UnavailableRoot;

        [NonSerialized] public InvItem CachedItem;
    }
}