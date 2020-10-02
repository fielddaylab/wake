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

        public string id { get; set; }
        public string linkTag { get; set; }

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

        public void InitializeLinkDependencies(LinkManager inLinkManager, DropSlot inDropSlot)
        {
            linkManager = inLinkManager;
            dropSlot = inDropSlot;
        }

        public void InitializeLinkData(string inId, string inTag, string inDisplayText)
        {
            id = inId;
            linkTag = inTag;
            displayText.SetText(inDisplayText);
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
