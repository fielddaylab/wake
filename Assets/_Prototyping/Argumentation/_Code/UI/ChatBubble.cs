using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Aqua;
namespace ProtoAqua.Argumentation
{
    public class ChatBubble : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler
    {
        // Node: NPC chat bubble
        // Link: User chat response

        [Header("Chat Bubble Dependencies")]
        [SerializeField] private Image bubbleImage;
        [SerializeField] private Image tailImage;

        [Header("Chat Bubble Settings")]
        [SerializeField] private bool chatBubble = false;
        [SerializeField] private bool isButton = false;
        [SerializeField, ShowIfField("isButton")] private BestiaryDescCategory m_FactCategory = BestiaryDescCategory.ALL;

        [Header("Colors")]
        [SerializeField] private Color m_DefaultColor = Color.cyan;
        [SerializeField] private Color m_ClickColor = Color.blue;

        [SerializeField] private TextMeshProUGUI displayText = null;

        private LinkManager linkManager = null;
        private Graph graph = null;

        public StringHash32 id { get; set; }
        public string linkTag { get; set; }
        public string typeTag { get; set; } //Organizes each bubble within the center tab

        private Routine colorRoutine;
        private bool pointerExit = false;

        public void SetChatBubble(bool isBubble) {
            chatBubble = isBubble;
        }

        public void ChangeColor(Color color)
        {
            bubbleImage.color = color;
            tailImage.color = color;
        }

        #region Link

        public void InitializeLinkDependencies(LinkManager inLinkManager, Graph inGraph)
        {
            linkManager = inLinkManager;
            graph = inGraph;
        }

        public void InitializeLinkData(StringHash32 inId, string inTag, string inType, string inDisplayText)
        {
            id = inId;
            linkTag = inTag;
            typeTag = inType;
            displayText.SetText(inDisplayText);
        }

        public void InitializeNodeData(StringHash32 inId, string inDisplayText)
        {
            id = inId;
            displayText.SetText(inDisplayText);
        }
        
        public void InitializeLinkData(string inDisplayText)
        {
            displayText.SetText(inDisplayText);
        }
        
        public void SetLongText()
        {
            Link currLink = graph.FindLink(id);
            displayText.SetText(currLink.DisplayText);
        }

        #endregion // Link

        #region Events

        public void OnPointerUp(PointerEventData eventData) {
            bubbleImage.SetColor(m_DefaultColor);
            if(tailImage != null) tailImage.SetColor(m_DefaultColor);

            if (!pointerExit)
            {
                if (chatBubble)
                {
                    colorRoutine.Replace(this, OnPointerUpColorRoutine());
                    Services.Events.Dispatch(ChatManager.Event_ArgumentBubbleSelection, eventData.pointerPress.GetComponent<ChatBubble>());
                }
                else if (isButton)
                {
                    colorRoutine.Replace(this, OnPointerUpColorRoutine());
                    Services.Events.Dispatch(ChatManager.Event_OpenBestiaryRequest, m_FactCategory);
                }

            }

        }

        public void OnPointerDown(PointerEventData eventData) {
            pointerExit = false;
        }

        public void OnPointerExit(PointerEventData eventData) {
            pointerExit = true;
        }

        #endregion // Events

        #region Routines

        private IEnumerator OnPointerDownColorRoutine()
        {
            bubbleImage.SetColor(m_DefaultColor);
            if(tailImage != null) {
                yield return Routine.Combine(bubbleImage.ColorTo(m_ClickColor, 0.1f), tailImage.ColorTo(m_ClickColor, 0.1f));
            }
            else {
                Routine.Start(bubbleImage.ColorTo(m_ClickColor, 0.1f));
            }
            
            
        }

        private IEnumerator OnPointerUpColorRoutine()
        {
            if(tailImage != null) {
                yield return Routine.Combine(bubbleImage.ColorTo(m_DefaultColor, 0.1f), tailImage.ColorTo(m_DefaultColor, 0.1f));
            }
            else {
                yield return Routine.Start(bubbleImage.ColorTo(m_DefaultColor, 0.1f));
            }
            
        }



        #endregion // Routines

        
    }
}
