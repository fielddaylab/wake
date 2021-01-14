using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Argumentation
{
    public class ClickableObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Clickable Object Dependencies")]
        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private Image m_BubbleImage; //Main Bubble Image
        [SerializeField] private Image m_TailImage; //Image for the small triangle tail

        [Header("Clickable Object Settings")]
        [SerializeField] private bool chatBubble = false;
        [SerializeField] private bool isButton = false;
        [SerializeField] private Color m_DefaultColor = Color.cyan;
        [SerializeField] private Color m_ClickColor = Color.blue;


        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Routine colorRoutine;
        private bool pointerExit = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            m_Canvas = rectTransform.GetCanvas();

        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerExit = false;
            colorRoutine.Replace(this, OnPointerDownColorRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_BubbleImage.SetColor(m_DefaultColor);
            m_TailImage.SetColor(m_DefaultColor);

            if (!pointerExit)
            {
                if (chatBubble)
                {
                    colorRoutine.Replace(this, OnPointerUpColorRoutine());
                    Services.Events.Dispatch("ArgumentationChatBubbleSelection", eventData.pointerPress.gameObject);
                }
                else if (isButton)
                {
                    colorRoutine.Replace(this, OnPointerUpColorRoutine());
                    Services.Events.Dispatch("OpenBestiaryWithFacts", eventData.pointerPress.gameObject);
                }

            }

        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerExit = true;
        }


        #region Routines


        private IEnumerator OnPointerDownColorRoutine()
        {
            m_BubbleImage.SetColor(m_DefaultColor);
            yield return Routine.Combine(m_BubbleImage.ColorTo(m_ClickColor, 0.1f), m_TailImage.ColorTo(m_ClickColor, 0.1f));
        }

        private IEnumerator OnPointerUpColorRoutine()
        {
            yield return Routine.Combine(m_BubbleImage.ColorTo(m_DefaultColor, 0.1f), m_TailImage.ColorTo(m_DefaultColor, 0.1f));
        }


        #endregion // Routines
    }
}
