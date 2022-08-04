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
    public sealed class JournalPageButton : MonoBehaviour {
        public RectTransform Rect;
        public Button Button;
        [Range(-1, 1)] public int Direction = 1;
    }
}