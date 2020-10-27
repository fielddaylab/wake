using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation 
{
    [RequireComponent(typeof(DraggableObject))]
    public class ChatBubble : MonoBehaviour {
        // Node: NPC chat bubble
        // Link: User chat response

        [Header("Chat Bubble Dependencies")]
        [SerializeField] private Image bubbleImage;
        [SerializeField] private TextMeshProUGUI displayText = null;

        private LinkManager linkManager = null;
        private DropSlot dropSlot = null;
        private Graph graph = null;
        

        public string id { get; set; }
        public string linkTag { get; set; }
        public string typeTag { get; set; } //Organizes each bubble within the center tab

        private DraggableObject draggableObject = null;

        private Routine colorRoutine;

        private void Start()
        {
            draggableObject = GetComponent<DraggableObject>();

            if (draggableObject) 
            {
                draggableObject.endDrag = EndDrag;
            }

           
        }

        public void ChangeColor(Color color)
        {
            bubbleImage.color = color;
        }

        public void InitializeLinkDependencies(LinkManager inLinkManager, DropSlot inDropSlot, Graph inGraph)
        {
            linkManager = inLinkManager;
            dropSlot = inDropSlot;
            graph = inGraph;
        }

        public void InitializeLinkData(string inId, string inTag, string inType, string inDisplayText)
        {
            id = inId;
            linkTag = inTag;
            typeTag = inType;
            displayText.SetText(inDisplayText);
        }

        public void SetLongText() {
            Link currLink = graph.FindLink(id);
            displayText.SetText(currLink.DisplayText);
        }

        public void InitializeNodeData(string inId, string inDisplayText)
        {
            id = inId;
            displayText.SetText(inDisplayText);
        }

        private void EndDrag(GameObject gameObject) 
        {
            linkManager.ResetLink(gameObject, id, true);
        }
    }
}
