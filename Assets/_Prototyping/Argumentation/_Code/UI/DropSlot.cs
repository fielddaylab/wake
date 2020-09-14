using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class DropSlot : MonoBehaviour, IDropHandler
    {
        [Serializable] public class DropEvent : UnityEvent<GameObject> { }
        public DropEvent OnDropped;
        
        [SerializeField] ScrollRect scrollRect;

        public void OnDrop(PointerEventData eventData)
        {
            //Fixed a bug where dragging the scroll rect would activate this function
            if(eventData.pointerDrag.name.Equals("ChatBox")) 
            {
                return;
            }
            if (eventData.pointerDrag != null)
            {
                OnDropped.Invoke(eventData.pointerDrag.gameObject);
                StartCoroutine(ScrollToBottom()); //TODO Add this when holding
                
            }
        }

        IEnumerator ScrollToBottom() 
        {
            yield return new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scrollRect.transform);
        }

        //Acts the same as dropping but hold the button
        public void OnHold(GameObject gameObject) 
        {
            OnDropped.Invoke(gameObject);
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}
