using System;
using System.Collections;
using Aqua;
using Aqua.Portable;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{

    public class ChatManager : MonoBehaviour
    {
        static public readonly StringHash32 Event_ArgumentBubbleSelection = "ArgumentationChatBubbleSelection";
        static public readonly StringHash32 Event_OpenBestiaryRequest = "OpenBestiaryWithFacts";

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

        [Header("Layers")]
        [SerializeField] private Transform m_Group = null;
        [SerializeField] private Transform m_NotAvailableGroup = null;

        private Routine invalidResponseRoutine;

        private void Start()
        {
            m_NodePool.Initialize();

            Services.Events.Register<ChatBubble>(Event_ArgumentBubbleSelection, OnDrop, this);
            Services.Events.Register<BestiaryDescCategory>(Event_OpenBestiaryRequest, OpenBestiary, this);

            m_Graph.OnGraphLoaded += Init;
            m_Graph.OnGraphNotAvailable += NotAvailable;
        }

        private void Init()
        {
            Routine.StartCall(this, OnStartChat);
        }

        private void NotAvailable()
        {
            m_Group.gameObject.SetActive(false);
            m_NotAvailableGroup.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnStartChat()
        {
            // Create the root node
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(m_Graph.RootNode.Id, m_Graph.RootNode.DisplayText);
            m_LinkManager.HandleNode(m_Graph.RootNode);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
        }

        // Activates when an item is dropped (called from DropSlot.cs)
        private void OnDrop(ChatBubble response)
        {
            // Make sure the object is draggable (This should never occur that its not)

            StringHash32 linkId = response.id;
            string linkTag = response.linkTag;

            if (linkTag == "claim")
            {
                response = m_LinkManager.CopyLink(m_Graph.FindLink(linkId));
            }

            // Place response in the chat grid and align it to the right
            response.transform.SetParent(m_ChatGrid);
            response.SetLongText();
            response.transform.GetChild(0).gameObject.GetComponent<VerticalLayoutGroup>()
                .childAlignment = TextAnchor.UpperRight;


            Routine.Start(this, ScrollRoutine(linkId, response));


        }

        private void OnSelect(ChatBubble selectedFact, StringHash32 linkId) {
            
            selectedFact.transform.SetParent(m_ChatGrid);
            selectedFact.transform.GetChild(0).gameObject.GetComponent<VerticalLayoutGroup>()
                .childAlignment = TextAnchor.UpperRight;
            selectedFact.gameObject.SetActive(true);
            Routine.Start(this, ScrollRoutine(linkId, selectedFact));
        }

        private void OpenBestiary(BestiaryDescCategory inCategory) {
            var future = BestiaryApp.RequestFact(inCategory);
            future.OnComplete( (s) => {
                Debug.Log("Selected: " + s.Fact.name);
                ChatBubble newLink = m_LinkManager.ClickBestiaryLink(s);
                OnSelect(newLink, s.Fact.Id());
                //m_FactText.SetText("Selected: " + s.Fact.GenerateSentence(s));
            }).OnFail(() => {
                //m_FactText.SetText("Selected: Nothing");
            });
        }

        // Add functionality to respond with more nodes, etc. This is where the NPC "talks back"
        private void RespondWithNextNode(StringHash32 linkId, ChatBubble response)
        {
            Node nextNode = m_Graph.NextNode(linkId);

            // Create the node bubble, and set its properties
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(nextNode.Id, nextNode.DisplayText);
            m_LinkManager.HandleNode(nextNode);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            // If the end node is reached, end the conversation
            if (newNode.id.Equals(m_Graph.EndNodeId))
            {
                newNode.ChangeColor(m_EndColor);
                EndConversationPopup();
            }

            // If the response is invalid, respond with an invalid node
            if (nextNode.IsInvalid)
            {
                newNode.ChangeColor(m_InvalidColor);
                // Add response back into list for reuse
                // m_LinkManager.ResetLink(response, linkId, false); (Not needed for bestiary)
                invalidResponseRoutine.Replace(this, InvalidResponseRoutine(response));
            }
            else
            {
                // If the response is valid, remove it from the player's available responses
                m_LinkManager.RemoveResponse(response);
            }

            if (nextNode.NextNodeId != null)
            {
                Routine.Start(this, ScrollAnotherNode(nextNode.NextNodeId));
            }
        }

        private void RespondWithAnotherNode(StringHash32 nextNodeId)
        {
            Node nextNode = m_Graph.FindNode(nextNodeId);
            // Create the node bubble, and set its properties
            ChatBubble newNode = m_NodePool.Alloc(m_ChatGrid);
            newNode.InitializeNodeData(nextNode.Id, nextNode.DisplayText);
            m_LinkManager.HandleNode(nextNode);
            m_Graph.SetCurrentNode(nextNode);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);
        }

        // Display a popup indicating that the end of the conversation has been reached
        private void EndConversationPopup()
        {
            Routine.Start(this, CompleteConversation());
        }

        private IEnumerator CompleteConversation()
        {
            m_InputRaycasterLayer.Override = false;

            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", Services.Data.CurrentJobId());
                yield return Services.Script.TriggerResponse("ArgumentationComplete");
            }

            Services.Data.Profile.Jobs.MarkComplete(Services.Data.CurrentJob());

            if (!Services.Script.IsCutscene())
            {
                Services.UI.Popup.Display("Congratulations!", "Job complete!")
                    .OnComplete((s) => {
                        StateUtil.LoadPreviousSceneWithWipe();
                    });
            }
        }

        // Shake response back and forth in the chat, indicating that the response is invalid
        private IEnumerator InvalidResponseRoutine(ChatBubble response)
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
        private IEnumerator ScrollRoutine(StringHash32 linkId, ChatBubble response)
        {
            m_InputRaycasterLayer.Override = false;

            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            yield return m_ScrollTime;
            RespondWithNextNode(linkId, response);
            UpdateButtonList(linkId);



            yield return m_ScrollRect.NormalizedPosTo(0, .5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            m_InputRaycasterLayer.Override = null;
        }

        //Handles scrolling when another node follows an NPC node
        //This does not update the button list and simply creates anothern ode to coninue the conversation
        private IEnumerator ScrollAnotherNode(StringHash32 nextNodeId)
        {
            m_InputRaycasterLayer.Override = false;

            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            yield return m_ScrollTime * 2;
            RespondWithAnotherNode(nextNodeId);


            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_ScrollRect.transform);

            m_InputRaycasterLayer.Override = null;
        }


        private void UpdateButtonList(StringHash32 linkId)
        {
            Link currentLink = m_Graph.FindLink(linkId);

            //@TODO FIX THIS
            if(currentLink == null) {
                return;
            }
            if (currentLink.Tag == "claim")
            {
                m_LinkManager.SelectClaim(linkId);
            }
        }


    }
}
