using System;
using System.Collections;
using Aqua.Animation;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NativeUtils;

namespace Aqua
{
    public class NativeToggle : Toggle, INativePointerDownHandler, INativePointerClickHandler
    {
        public override void OnPointerClick(PointerEventData eventData) {
        }

        public virtual void OnNativePointerClick(PointerEventData eventData) {
            base.OnPointerClick(eventData);
        }

        public void OnNativePointerDown(PointerEventData eventData) {
        }
    }
}