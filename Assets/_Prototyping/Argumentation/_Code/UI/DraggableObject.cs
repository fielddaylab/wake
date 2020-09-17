using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class DraggableObject : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Draggable Object Dependencies")]
        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private DropSlot m_DropSlot;
        [SerializeField] private Image m_BubbleImage;

        [Serializable] public class DragEvent : UnityEvent<GameObject> { }
        public DragEvent EndDrag;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool dragging = false;

        private Routine holdMouseRoutine;
        private Routine colorRoutine;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            m_Canvas = rectTransform.GetCanvas();
            m_DropSlot = GameObject.Find("ChatBox").GetComponent<DropSlot>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            colorRoutine.Replace(this, InitializeColorRoutine());
            holdMouseRoutine = Routine.StartDelay(this, HoldMouse, 1);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            holdMouseRoutine.Stop();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = false;
            dragging = true;
            colorRoutine.Replace(this, InitializeColorRoutine());
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
            colorRoutine.Replace(this, OnEndDragColorRoutine());
            EndDrag.Invoke(eventData.pointerDrag.gameObject);
        }

        private void HoldMouse() 
        {
            //Don't want to activate the press and hold if dragging
            if (!dragging) 
            {
                m_DropSlot.OnHold(this.gameObject);
            }
        }

        private IEnumerator HoldMouseRoutine()
        {
            yield return 1.0f;
            HoldMouse();
        }

        private IEnumerator OnEndDragColorRoutine()
        {
            yield return m_BubbleImage.ColorTo(Color.cyan, 0.1f);
        }

        private IEnumerator InitializeColorRoutine()
        {
            m_BubbleImage.SetColor(Color.cyan);
            yield return m_BubbleImage.ColorTo(Color.blue, 0.1f);
        }
    }
}
