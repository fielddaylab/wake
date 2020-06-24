using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Aqua
{
    public class PointerListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        #region Types

        [Serializable]
        public class BaseEvent : UnityEvent<BaseEventData> { }

        [Serializable]
        public class PointerEvent : UnityEvent<PointerEventData> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private PointerEvent m_OnPointerEnter = new PointerEvent();
        [SerializeField] private PointerEvent m_OnPointerExit = new PointerEvent();
        [SerializeField] private PointerEvent m_OnPointerDown = new PointerEvent();
        [SerializeField] private PointerEvent m_OnPointerUp = new PointerEvent();
        [SerializeField] private BaseEvent m_OnSelect = new BaseEvent();
        [SerializeField] private BaseEvent m_OnDeselect = new BaseEvent();

        #endregion // Inspector

        [NonSerialized] private readonly HashSet<int> m_EnteredPointers;
        [NonSerialized] private readonly HashSet<int> m_PressedPointers;
        [NonSerialized] private bool m_Selected;

        public PointerEvent onPointerEnter { get { return m_OnPointerEnter; } }
        public PointerEvent onPointerExit { get { return m_OnPointerExit; } }
        public PointerEvent onPointerDown { get { return m_OnPointerDown; } }
        public PointerEvent onPointerUp { get { return m_OnPointerUp; } }

        public BaseEvent onSelect { get { return m_OnSelect; } }
        public BaseEvent onDeselect { get { return m_OnDeselect; } }

        #region Handlers

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (!m_PressedPointers.Add(eventData.pointerId))
                return;
            
            m_OnPointerDown.Invoke(eventData);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (!m_PressedPointers.Remove(eventData.pointerId))
                return;

            m_OnPointerUp.Invoke(eventData);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (!m_EnteredPointers.Add(eventData.pointerId))
                return;

            m_OnPointerEnter.Invoke(eventData);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!m_EnteredPointers.Remove(eventData.pointerId))
                return;

            m_OnPointerExit.Invoke(eventData);
        }

        void ISelectHandler.OnSelect(BaseEventData eventData)
        {
            if (m_Selected)
                return;
            
            m_Selected = true;
            m_OnSelect.Invoke(eventData);
        }

        void IDeselectHandler.OnDeselect(BaseEventData eventData)
        {
            if (!m_Selected)
                return;

            m_Selected = false;
            m_OnDeselect.Invoke(eventData);
        }

        #endregion // Handlers
    }
}