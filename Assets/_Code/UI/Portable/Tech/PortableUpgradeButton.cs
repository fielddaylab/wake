using UnityEngine;
using BeauPools;
using Aqua.Profile;
using UnityEngine.UI;
using BeauUtil;
using BeauUtil.UI;
using BeauRoutine;
using System.Collections;
using System;

namespace Aqua.Portable {
    public class PortableUpgradeButton : MonoBehaviour {
        public Button Button;
        public ColorGroup Outline;
        public PointerListener Listener;

        [Header("Info")]
        public Image Icon;
        public LocText Title;
        public CursorInteractionHint Cursor;

        [NonSerialized] public InvItem CachedItem;
    }
}