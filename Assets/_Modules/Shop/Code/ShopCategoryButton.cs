using System;
using Aqua.Cameras;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Shop {
    public class ShopCategoryButton : MonoBehaviour {
        public Toggle Toggle;
        public ActiveGroup Group;
        public AppearAnim Anim;

        [Header("Left Column")]
        public TextId LeftHeader;
        [ItemId] public StringHash32[] LeftItems;

        [Header("RightColumn")]
        public TextId RightHeader;
        [ItemId] public StringHash32[] RightItems;

        [Header("Components")]
        [InstanceOnly] public CameraPose CameraPose;
    }
}