using UnityEngine;
using BeauUtil;
using Aqua.Cameras;
using System.Collections;
using BeauRoutine;
using BeauUtil.UI;
using UnityEngine.EventSystems;
using System;
using BeauRoutine.Splines;
using TMPro;

namespace Aqua.Shop {
    public class ShopItem : MonoBehaviour
    {
        [ItemId] public SerializedHash32 ItemId = default;
        public PointerListener Clickable;
        public CursorInteractionHint Tooltip;
        public GameObject AvailableRoot;
        public GameObject SoldOutRoot;
        public LocText ItemNameText;
        public GameObject CoinsRoot;
        public TMP_Text CoinsText;
        public GameObject GearsRoot;
        public TMP_Text GearsText;

        [NonSerialized] public InvItem CachedItem;
    }
}