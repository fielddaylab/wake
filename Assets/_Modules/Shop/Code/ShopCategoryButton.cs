using UnityEngine;
using BeauUtil;
using BeauUtil.UI;
using System;
using TMPro;
using UnityEngine.UI;
using Aqua.Cameras;

namespace Aqua.Shop
{
    public class ShopCategoryButton : MonoBehaviour
    {
        public Toggle Toggle;
        public ActiveGroup Group;

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