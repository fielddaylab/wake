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
        
        [Header("Chat Manager Settings")]
        [SerializeField] private float m_ScrollTime = 0.25f;
        [SerializeField] private Color m_InvalidColor = Color.red;
        [SerializeField] private Color m_EndColor = Color.green;

        private DropSlot dropSlot;

        private Routine invalidResponseRoutine;
        
        private void Start()
        {
            m_NodePool.Initialize();

            // Set up the listener for Drop Slot
            dropSlot = GetComponent<DropSlot>();
            dropSlot.onDropped += OnDrop;

            // Create the root node
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(m_Graph.RootNode.Id, m_Graph.RootNode.DisplayText);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
        }

        // Activates when an item is dropped (called from DropSlot.cs)
        private void OnDrop(GameObject response) 
        {
            // Make sure the object is draggable (This should never occur that its not)
            if (response.GetComponent<DraggableObject>() == null) 
            {
                return;
            }

            // Place response in the chat grid and align it to the right
            response.transform.SetParent(m_ChatGrid);
            response.GetComponent<DraggableObject>().enabled = false;
            response.transform.GetChild(0).gameObject.GetComponent<VerticalLayoutGroup>()
                .childAlignment = TextAnchor.UpperRight;

            string linkId = response.GetComponent<ChatBubble>().id;
            string linkTag = response.GetComponent<ChatBubble>().linkTag;


            Routine.Start(ScrollRoutine(linkId, response));

            
        }

        // Add functionality to respond with more nodes, etc. This is where the NPC "talks back"
        private void RespondWithNextNode(string linkId, GameObject response) 
        {
            Node nextNode = m_Graph.NextNode(linkId);
            Link currentLink = m_Graph.FindLink(linkId);

            CheckConditionsMet(nextNode, currentLink);

            // Create the node bubble, and set its properties
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(nextNode.Id, nextNode.DisplayText);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            // If the end node is reached, end the conversation
            if (newNode.id.Equals(m_Graph.EndNodeId))
            {
                newNode.ChangeColor(m_EndColor);
                EndConversationPopup();
            } 
            
            // If the response is invalid, respond with an invalid node
            if (newNode.id.Contains("invalid"))
            {
                newNode.ChangeColor(m_InvalidColor);
                // Add response back into list for reuse
                m_LinkManager.ResetLink(response, linkId, false);
                invalidResponseRoutine.Replace(this, InvalidResponseRoutine(response));
            }
            else
            {
                // If the response is valid, remove it from the player's available responses
                m_LinkManager.RemoveResponse(response);
            }

            if(nextNode.NextNodeId != null) {
                RespondWithAnotherNode(nextNode.NextNodeId);
            }
        }

        private void RespondWithAnotherNode(string nextNodeId) {
            Node nextNode = m_Graph.FindNode(nextNodeId);
            // Create the node bubble, and set its properties
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(nextNode.Id, nextNode.DisplayText);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
        }

        // Checks if conditions for the next node are met. If not, create and respond with
        // a node indicate that conditions haven't been met
        private void CheckConditionsMet(Node nextNode, Link currentLink)
        {
            if (!m_Graph.Conditions.CheckConditions(nextNode, currentLink))
            {
                ChatBubble conditionsNotMetNode = m_NodePool.Alloc(m_ChatGrid);
                conditionsNotMetNode.InitializeNodeData("conditionsNotMet", "Conditions not met!");
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
            }
        }

        // Display a popup indicating that the end of the conversation has been reached
        private void EndConversationPopup()
        {
            NamedOption[] options = { new NamedOption("Continue") };
            Services.UI.Popup().Present("Congratulations!", "End of conversation", options);
        }

        // Shake response back and forth in the chat, indicating that the response is invalid
        private IEnumerator InvalidResponseRoutine(GameObject response)
        {
            yield return response.transform.MoveTo(transform.position + 
                            new Vector3(0.1f, 0, 0), 0.05f, Axis.X).Ease(Curve.CubeOut);
            yield return response.transform.MoveTo(transform.position + 
                            new Vector3(-0.1f, 0, 0), 0.05f, Axis.X).Ease(Curve.CubeOut);
            yield return response.transform.MoveTo(transform.position, 
                            0.05f, Axis.X).Ease(Curve.CubeOut);
        }

        // Handles scrolling when the response is placed in the chat, and once again when the
        // NPC responds with the next node. Raycasting is disabled during the routine so that
        // the player can't drag in additional responses.
        private IEnumerator ScrollRoutine(string linkId, GameObject response)
        {
            m_InputRaycasterLayer.OverrideState(false);

            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            yield return m_ScrollTime;
            RespondWithNextNode(linkId, response);
            UpdateButtonList(linkId);


            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            m_InputRaycasterLayer.ClearOverride();
        }

        private void UpdateButtonList(string linkId) {
            Link currentLink = m_Graph.FindLink(linkId);
            if(currentLink.Tag == "claim") {
                m_LinkManager.SelectClaim(linkId);
            }
        }

        
    }
}
