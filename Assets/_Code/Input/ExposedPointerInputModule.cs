using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Aqua
{
    public enum PointerInputMode : byte
    {
        Mouse,
        Touch
    }

    public class ExposedPointerInputModule : StandaloneInputModule
    {
        private PointerEventData m_MousePointerData;
        private PointerEventData m_TouchPointerData;

        private PointerInputMode m_InputMode;
        private bool m_IsEditingText;

        public event Action<PointerInputMode> OnModeChanged;

        public PointerEventData GetPointerEventData()
        {
            if (m_InputMode == PointerInputMode.Touch)
            {
                if (m_TouchPointerData == null)
                    GetPointerData(0, out m_TouchPointerData, true);
                return m_TouchPointerData;
            }

            if (m_MousePointerData == null)
                GetPointerData(kMouseLeftId, out m_MousePointerData, true);
            return m_MousePointerData;
        }

        public bool IsPointerOverCanvas()
        {
            PointerEventData eventData = GetPointerEventData();

            if (eventData != null)
            {
                var baseRaycaster = eventData.pointerCurrentRaycast.module;
                return !baseRaycaster.IsReferenceNull() && baseRaycaster is GraphicRaycaster;
            }

            return false;
        }

        public void FindNativeClick(float x, float y, PointerEventData data) {
            data.position = new Vector2(x * Screen.width, y * Screen.height);
            eventSystem.RaycastAll(data, m_RaycastResultCache);
            data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
        }

        public bool IsEditingText()
        {
            return m_IsEditingText;
        }

        public override void Process()
        {
            base.Process();

            if (Input.touchCount > 0 || !Input.mousePresent) // touchscreen input signals touch mode
            {
                SetInputMode(PointerInputMode.Touch);
            }
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) // any mouse buttons signals mouse mode
            {
                SetInputMode(PointerInputMode.Mouse);
            }

            m_IsEditingText = false;
            GameObject currentFocus = eventSystem.currentSelectedGameObject;
            if (currentFocus != null)
            {
                TMP_InputField inputField = currentFocus.GetComponent<TMP_InputField>();
                if (inputField)
                {
                    m_IsEditingText = inputField.isFocused;
                }
            }
        }

        public override void DeactivateModule()
        {
            base.DeactivateModule();
            m_TouchPointerData = null;
            m_MousePointerData = null;
        }

        private void SetInputMode(PointerInputMode inMode)
        {
            if (m_InputMode != inMode)
            {
                m_InputMode = inMode;
                OnModeChanged?.Invoke(inMode);
            }
        }
    }
}