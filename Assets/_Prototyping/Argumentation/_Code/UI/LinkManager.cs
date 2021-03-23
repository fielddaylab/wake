using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class LinkManager : MonoBehaviour
    {
        [Serializable]
        public class LinkPool : SerializablePool<ChatBubble> { }

        [Header("Link Manager Dependencies")]
        [SerializeField] private Graph m_Graph = null;
        [SerializeField] private GameObject m_LinkContainer = null;
        [SerializeField] private LinkPool m_LinkPool = null;

        [Header("Button Dependencies")]
        [SerializeField] private GameObject m_TagButtons = null;
        [SerializeField] private Button m_BehaviorsButton = null;
        [SerializeField] private Button m_EcosystemsButton = null;
        [SerializeField] private Button m_ModelsButton = null;

        [NonSerialized] private List<GameObject> responses = new List<GameObject>();
        private StringHash32 currentClaim = "";
        private bool claimSelected = false;

        private void Start()
        {
            m_LinkPool.Initialize();

            // m_BehaviorsButton.onClick.AddListener(() => ToggleTabs("behavior"));
            // m_EcosystemsButton.onClick.AddListener(() => ToggleTabs("ecosystem"));
            // m_ModelsButton.onClick.AddListener(() => ToggleTabs("model"));

            m_Graph.OnGraphLoaded += Init;
        }

        private void Init()
        {
            // Create links for each Link in the dictionary of the graph
            foreach (KeyValuePair<StringHash32, Link> link in m_Graph.LinkDictionary)
            {
                Link currLink = link.Value;
                if(currLink.Tag == "claim") {
                    CreateLink(currLink);
                }
                
            }
        }

        public GameObject ClickBestiaryLink(PlayerFactParams s) {
            ChatBubble newLink = m_LinkPool.Alloc();
            newLink.gameObject.SetActive(false);
            Link link = m_Graph.FindLink(s.FactId);
            if (link != null)
                newLink.InitializeLinkData(link.DisplayText);
            else
                newLink.InitializeLinkData(s.Fact.GenerateSentence());
            return newLink.gameObject;
        }

        // Reset a given response once used. If the response isn't placed in the chat,
        // delete will be true and the object can be freed from the pool before being
        // reallocated by CreateLink
        public void ResetLink(GameObject gameObject, StringHash32 linkId, bool delete)
        {
            return;
            // RemoveResponse(gameObject);

            // if (delete)
            // {
            //     m_LinkPool.Free(gameObject.GetComponent<ChatBubble>());
            // }

            // CreateLink(m_Graph.FindLink(linkId));
            // LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_LinkContainer.transform);
        }

        // Helper function for removing a response from the responses list
        public void RemoveResponse(GameObject gameObject)
        {
            responses.Remove(gameObject);
        }

        public void ToggleType(string type)
        {
            foreach (GameObject gameObject in responses)
            {
                ChatBubble chatBubble = gameObject.GetComponent<ChatBubble>();

                if (chatBubble.typeTag.Equals(type))
                {
                    gameObject.SetActive(true);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public GameObject CopyLink(Link link)
        {
            return CreateLink(link);
        }

        // Allocate a new link from the pool and initialize its fields based on data from the graph
        private GameObject CreateLink(Link link)
        {
            ChatBubble newLink = m_LinkPool.Alloc(m_LinkContainer.transform);
            newLink.InitializeLinkDependencies(this, m_Graph);



            //TODO remove this check, temporary
            if (link.ShortenedText != null)
            {
                newLink.InitializeLinkData(link.Id, link.Tag, link.Type, link.ShortenedText);
            }
            else
            {
                newLink.InitializeLinkData(link.Id, link.Tag, link.Type, link.DisplayText);
            }

            newLink.transform.SetSiblingIndex((int) link.Index);

            responses.Add(newLink.gameObject);
            return newLink.gameObject;
        }

        // Show responses with a given tag and hide all other responses
        private void ToggleTabs(string tagToShow)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_LinkContainer.transform);
        }

        public void SelectClaim(StringHash32 linkId)
        {
            currentClaim = linkId;
            HideClaims();
        }

        public void HandleNode(Node inNode)
        {
            if (inNode.ShowClaims)
            {
                ShowClaims();
            }
            else
            {
                HideClaims();
            }
        }

        private void ShowClaims()
        {
            m_LinkContainer.SetActive(true);
            ToggleTabs("claim");
            ToggleType("claim");
            HideTabs();
        }

        private void HideClaims()
        {
            ShowTabs();
            m_LinkContainer.SetActive(false);
        }

        private void HideTabs()
        {
            m_TagButtons.SetActive(false);
        }

        private void ShowTabs()
        {
            m_TagButtons.SetActive(true);
        }

    }
}
