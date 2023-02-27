using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    public class GenericPanel : BasePanel {
        [SerializeField] private AppearAnim m_AppearAnim = null;

        protected override IEnumerator TransitionToShow() {
            Root.gameObject.SetActive(true);
            yield return m_AppearAnim.Ping();
        }

        protected override IEnumerator TransitionToHide() {
            yield return CanvasGroup.FadeTo(0, 0.2f);
            Root.gameObject.SetActive(false);
        }
    }
}