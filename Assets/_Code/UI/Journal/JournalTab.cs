using System;
using System.Collections.Generic;
using Aqua.Compression;
using Aqua.Journal;
using BeauPools;
using BeauUtil;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public sealed class JournalTab : MonoBehaviour {
        public RectTransform Rect;
        public Toggle Toggle;
        public JournalCategoryMask Category;
    }
}