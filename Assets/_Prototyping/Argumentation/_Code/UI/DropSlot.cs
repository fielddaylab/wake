using System;
using BeauRoutine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class DropSlot : MonoBehaviour, IDropHandler
    {
        [Serializable] public class DropEvent : UnityEvent<GameObject> { }

        [Header("Drop Slot Dependencies")]
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform rectTransform;

        public DropEvent OnDropped;

        private Routine scrollRoutine;
        
        public void OnDrop(PointerEventData eventData)
        {
            //Fixed a bug where dragging the scroll rect would activate this function
            if (eventData.pointerDrag.name.Equals("ChatBox")) 
            {
                return;
            }

            if (eventData.pointerDrag != null)
            {
                OnDropped.Invoke(eventData.pointerDrag.gameObject);
            }
        }
        
        //Acts the same as dropping but hold the button
        public void OnHold(GameObject gameObject) 
        {
            OnDropped.Invoke(gameObject);
        }
    }
}
