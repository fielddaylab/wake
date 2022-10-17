using System;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable {
    public class TechCategoryButton : MonoBehaviour {
        public Toggle Toggle;
        public ActiveGroup Group;

        [Header("Left Column")]
        public TextId LeftHeader;
        [ItemId] public StringHash32[] LeftItems;

        [Header("RightColumn")]
        public TextId RightHeader;
        [ItemId] public StringHash32[] RightItems;
    }
}