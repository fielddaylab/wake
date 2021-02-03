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

            Services.Events.Register<GameObject>("ArgumentationChatBubbleSelection", OnDrop, this);
            Services.Events.Register<GameObject>("OpenBestiaryWithFacts", OpenBestiary, this);

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
        private void OnDrop(GameObject response)
        {
            // Make sure the object is draggable (This should never occur that its not)
            if (response.GetComponent<ClickableObject>() == null)
            {
                return;
            }

            // Place response in the chat grid and align it to the right
            response.transform.SetParent(m_ChatGrid);
            response.GetComponent<ClickableObject>().enabled = false;
            response.GetComponent<ChatBubble>().SetLongText();
            response.transform.GetChild(0).gameObject.GetComponent<VerticalLayoutGroup>()
                .childAlignment = TextAnchor.UpperRight;

            StringHash32 linkId = response.GetComponent<ChatBubble>().id;
            string linkTag = response.GetComponent<ChatBubble>().linkTag;


            Routine.Start(this, ScrollRoutine(linkId, response));


        }

        private void OnSelect(GameObject selectedFact, StringHash32 linkId) {
            
            selectedFact.transform.SetParent(m_ChatGrid);
            selectedFact.GetComponent<ClickableObject>().enabled = false;
            selectedFact.transform.GetChild(0).gameObject.GetComponent<VerticalLayoutGroup>()
                .childAlignment = TextAnchor.UpperRight;
            selectedFact.SetActive(true);
            Routine.Start(this, ScrollRoutine(linkId, selectedFact));
        }

        private void OpenBestiary(GameObject clicked) {
            var request = new BestiaryApp.SelectFactRequest(BestiaryDescCategory.Critter);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            request.Return.OnComplete( (s) => {
                Debug.Log("Selected: " + s.Fact.name);
                GameObject newLink = m_LinkManager.ClickBestiaryLink(s);
                OnSelect(newLink, s.Fact.name);
                //m_FactText.SetText("Selected: " + s.Fact.GenerateSentence(s));
            }).OnFail(() => {
                //m_FactText.SetText("Selected: Nothing");
            });
        }

        // Add functionality to respond with more nodes, etc. This is where the NPC "talks back"
        private void RespondWithNextNode(StringHash32 linkId, GameObject response)
        {
            Node nextNode = m_Graph.NextNode(linkId);
            Link currentLink = m_Graph.FindLink(linkId);

            //CheckConditionsMet(nextNode, currentLink);

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
            Services.Data.Profile.Jobs.MarkComplete(Services.Data.CurrentJob());

            NamedOption[] options = { new NamedOption("Continue") };
            Services.UI.Popup.Present("Congratulations!", "Job complete!", options)
                .OnComplete((s) => {
                    Services.Script.TriggerResponse("ArgumentationComplete");
                    StateUtil.LoadPreviousSceneWithWipe();
                });
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
        private IEnumerator ScrollRoutine(StringHash32 linkId, GameObject response)
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
