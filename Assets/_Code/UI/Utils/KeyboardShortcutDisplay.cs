using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class KeyboardShortcutDisplay : MonoBehaviour
    {
        public Graphic[] Graphics;

        private void OnEnable() {
            Services.UI.RegisterShortcut(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterShortcut(this);
        }

        public void SetDisplay(bool active) {
            for(int i = 0; i < Graphics.Length; i++) {
                Graphics[i].enabled = active;
            }
        }
    }
}