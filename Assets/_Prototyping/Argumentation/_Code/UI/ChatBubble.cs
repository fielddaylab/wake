using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Argumentation {
    //Enum to know what type of chat bubble it is
    public enum BubbleType {
            Node,
            Link
    }

    [RequireComponent(typeof(DraggableObject))]
    public class ChatBubble : MonoBehaviour {
        //Node: NPC chat bubble
        //Link: User chat response

        [SerializeField] LinkManager linkManager = null;

        public BubbleType bubbleType { get; set; }
        public string id { get; set; }
        public string linkTag { get; set; }

        private DraggableObject draggableObject = null;


        // Start is called before the first frame update
        void Start()
        {
            //TODO Ask Autumn script execution order
            draggableObject = GetComponent<DraggableObject>();
            if(draggableObject) {
                draggableObject.EndDrag.AddListener(EndDrag);
            }
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private void EndDrag(GameObject gameObject) {
            linkManager.ResetLink(gameObject, id, true);
        }
    }

}

