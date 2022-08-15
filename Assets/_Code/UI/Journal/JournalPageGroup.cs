using System;
using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public sealed class JournalPageGroup : MonoBehaviour {
        public Mask Mask;
        public Image MaskImage;
        public CanvasGroup Group;
        public GameObject Prefab;

        public void DisableMasking() {
            Mask.enabled = false;
            MaskImage.enabled = false;
        }
    }
}