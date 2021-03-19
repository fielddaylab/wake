using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    [RequireComponent(typeof(Selectable))]
    public class UISounds : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        #region Inspector

        [SerializeField, HideInInspector] private Selectable m_Selectable = null;
        
        [Header("Hover State")]
        [SerializeField] private string m_EnterEvent = "ui_hover";
        [SerializeField] private string m_ExitEvent = null;
        
        [Header("Click State")]
        [SerializeField] private string m_DownEvent = null;
        [SerializeField] private string m_UpEvent = null;
        [SerializeField] private string m_ClickEvent = "ui_click";

        #endregion // Inspector

        [NonSerialized] private bool m_WasInteractable;

        private void TryPlay(string inEventId, bool inbCheckPrevState)
        {
            if (string.IsNullOrEmpty(inEventId))
                return;

            if (inbCheckPrevState)
            {
                if (!m_WasInteractable)
                    return;
            }
            else
            {
                if (!m_Selectable.IsInteractable())
                    return;
            }
            
            Services.Audio.PostEvent(inEventId);
        }

        #region Handlers

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            TryPlay(m_ClickEvent, true);
            m_WasInteractable = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            TryPlay(m_DownEvent, false);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            TryPlay(m_EnterEvent, false);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            TryPlay(m_ExitEvent, false);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            m_WasInteractable = m_Selectable.IsInteractable();
            TryPlay(m_UpEvent, false);
        }

        #endregion // Handlers

        #region Unity Events

        private void Awake()
        {
            if (!m_Selectable)
            {
                m_Selectable = GetComponent<Selectable>();
            }
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Selectable = GetComponent<Selectable>();
        }

        private void OnValidate()
        {
            if (!m_Selectable)
            {
                m_Selectable = GetComponent<Selectable>();
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    }
}