using UnityEngine;

namespace ProtoAqua.Argumentation 
{
    //Enum to know what type of chat bubble it is
    public enum BubbleType 
    {
            Node,
            Link
    }

    [RequireComponent(typeof(DraggableObject))]
    public class ChatBubble : MonoBehaviour {
        //Node: NPC chat bubble
        //Link: User chat response

        [Header("Chat Bubble Dependencies")]
        [SerializeField] LinkManager linkManager = null;

        public BubbleType bubbleType { get; set; }
        public string id { get; set; }
        public string linkTag { get; set; }

        private DraggableObject draggableObject = null;

        private void Start()
        {
            //TODO Ask Autumn script execution order
            draggableObject = GetComponent<DraggableObject>();

            if (draggableObject) 
            {
                draggableObject.EndDrag.AddListener(EndDrag);
            }
        }

        private void EndDrag(GameObject gameObject) 
        {
            linkManager.ResetLink(gameObject, id);
        }
    }
}
