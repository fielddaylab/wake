using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ProtoAqua.Argumentation
{
    public class DraggableObject : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Draggable Object Dependencies")]
        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private DropSlot dropSlot;

        [Serializable] public class DragEvent : UnityEvent<GameObject> { }
        public DragEvent EndDrag;


        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool dragging = false;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Invoke("HoldMouse", 1);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelInvoke("HoldMouse");
        }

        private void HoldMouse() 
        {
            //Don't want to activate the press and hold if dragging
            if(!dragging) {
                dropSlot.OnHold(this.gameObject);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = false;
            dragging = true;
            
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / m_Canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            dragging = false;
            //TODO do this more efficiently without destroying objects
            EndDrag.Invoke(eventData.pointerDrag.gameObject);
        }
    }
}
