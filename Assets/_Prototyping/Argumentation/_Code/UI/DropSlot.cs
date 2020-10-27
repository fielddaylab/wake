using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class DropSlot : MonoBehaviour, IDropHandler
    {
        [Header("Drop Slot Dependencies")]
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform rectTransform;

        public delegate void OnDropped(GameObject gameObject);
        public OnDropped onDropped;

        private Routine onDroppedRoutine;
        
        public void OnDrop(PointerEventData eventData)
        {
            // Fixed a bug where dragging the scroll rect would activate this function
            if (eventData.pointerDrag.name.Equals("ChatBox")) 
            {
                return;
            }

            if (eventData.pointerDrag != null)
            {
                //@TODO Change the implementation of this (Will remove draggable objects and this will change)
                eventData.pointerDrag.gameObject.GetComponent<ChatBubble>().SetLongText();
                onDroppedRoutine.Replace(this, OnDroppedRoutine(eventData.pointerDrag.gameObject));
            }
        }
        
        // Acts the same as dropping but hold the button
        public void OnHold(GameObject gameObject) 
        {
            onDroppedRoutine.Replace(this, OnDroppedRoutine(gameObject));
        }

        private IEnumerator OnDroppedRoutine(GameObject gameObject)
        {
            yield return 0.1f;
            onDropped(gameObject);
        }
    }
}
