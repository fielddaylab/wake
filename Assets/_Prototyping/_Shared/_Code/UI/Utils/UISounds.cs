using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua
{
    [RequireComponent(typeof(Selectable))]
    public class UISounds : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        #region Inspector

        [SerializeField, HideInInspector] private Selectable m_Selectable = null;
        
        [Header("Hover State")]
        [SerializeField] private string m_EnterEvent = null;
        [SerializeField] private string m_ExitEvent = null;
        
        [Header("Click State")]
        [SerializeField] private string m_DownEvent = null;
        [SerializeField] private string m_UpEvent = null;
        [SerializeField] private string m_ClickEvent = null;

        #endregion // Inspector

        private void TryPlay(string inEventId)
        {
            if (!string.IsNullOrEmpty(inEventId) && m_Selectable.IsInteractable())
                Services.Audio.PostEvent(inEventId);
        }

        #region Handlers

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            TryPlay(m_ClickEvent);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            TryPlay(m_DownEvent);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            TryPlay(m_EnterEvent);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            TryPlay(m_ExitEvent);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            TryPlay(m_UpEvent);
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