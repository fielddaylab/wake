using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation 
{
    [RequireComponent(typeof(DropSlot))]
    public class ChatManager : MonoBehaviour 
    {
        [Serializable]
        public class NodePool : SerializablePool<ChatBubble> { }

        [Header("Chat Manager Dependencies")]
        [SerializeField] private Graph m_Graph = null;
        [SerializeField] private LinkManager m_LinkManager = null;
        [SerializeField] private Transform m_ChatGrid = null;
        [SerializeField] private ScrollRect m_ScrollRect = null;
        [SerializeField] private InputRaycasterLayer m_InputRaycasterLayer = null;
        [SerializeField] private NodePool m_NodePool = null;
        
        private DropSlot dropSlot;
        
        // Start is called before the first frame update
        private void Start()
        {
            m_NodePool.Initialize();

            //Set up the listener for Drop Slot
            dropSlot = GetComponent<DropSlot>();
            dropSlot.OnDropped.AddListener(OnDrop);

            //Spawn some starting nodes
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.bubbleType = BubbleType.Node;
            newNode.InitializeNodeData(m_Graph.RootNode.Id, m_Graph.RootNode.DisplayText);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
        }

        //Activates when an item is dropped (called from DropSlot.cs)
        private void OnDrop(GameObject droppedItem) 
        {
            //Make sure the object is draggable (This should never occur that its not)
            if (droppedItem.GetComponent<DraggableObject>() == null) 
            {
                return;
            }

            droppedItem.transform.SetParent(m_ChatGrid); //Set it into the grid 
            droppedItem.GetComponent<DraggableObject>().enabled = false; //Make it no longer able to be dragged

            string linkId = droppedItem.GetComponent<ChatBubble>().id;
            Routine.Start(ScrollRoutine(linkId));
            
            // Add response back into list for reuse
            m_LinkManager.ResetLink(droppedItem, linkId, false);
        }

        //Rename, bad naming
        //Add functionality to respond with more nodes, etc. This is where the NPC "talks back"
        private void RespondWithNextNode(string factId) 
        {
            Node nextNode = m_Graph.NextNode(factId);
            Link currentLink = m_Graph.FindLink(factId);

            CheckConditionsMet(nextNode, currentLink);

            // Create the node bubble, and set its properties
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(nextNode.Id, nextNode.DisplayText);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            if (newNode.id.Equals(m_Graph.EndNodeId))
            {
                newNode.ChangeColor(Color.green);
                EndConversationPopup();
            } 
            else if (newNode.id.Contains("invalid")) // Change this
            {
                newNode.ChangeColor(Color.red);
            }
        }

        private void CheckConditionsMet(Node nextNode, Link currentLink)
        {
            if (!m_Graph.Conditions.CheckConditions(nextNode, currentLink))
            {
                ChatBubble conditionsNotMetNode = m_NodePool.Alloc(m_ChatGrid);
                conditionsNotMetNode.InitializeNodeData("conditionsNotMet", "Conditions not met!");
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
            }
        }

        private void EndConversationPopup()
        {
            NamedOption[] options = {new NamedOption("Continue")};
            Services.UI.Popup().Present("Congratulations!", "End of conversation", options);
        }

        private IEnumerator ScrollRoutine(string linkId)
        {
            m_InputRaycasterLayer.OverrideState(false);

            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            yield return 0.25f;
            RespondWithNextNode(linkId);

            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            m_InputRaycasterLayer.ClearOverride();
        }
    }
}
