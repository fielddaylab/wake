using System;
using System.Collections.Generic;
using BeauPools;
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
        [SerializeField] private DropSlot m_DropSlot = null;
        [SerializeField] private TypeManager m_TypeManager = null;

        [Header("Button Dependencies")]
        [SerializeField] private GameObject m_TagButtons = null;
        [SerializeField] private Button m_BehaviorsButton = null;
        [SerializeField] private Button m_EcosystemsButton = null;
        [SerializeField] private Button m_ModelsButton = null;

        private List<GameObject> responses = new List<GameObject>();
        private string currentClaim = "";
        private bool claimSelected = false;

        private void Start() 
        {
            m_LinkPool.Initialize();

            m_BehaviorsButton.onClick.AddListener(() => ToggleTabs("behavior"));
            m_EcosystemsButton.onClick.AddListener(() => ToggleTabs("ecosystem"));
            m_ModelsButton.onClick.AddListener(() => ToggleTabs("model"));

            // Create links for each Link in the dictionary of the graph
            foreach (KeyValuePair<string, Link> link in m_Graph.LinkDictionary) 
            {
               Link currLink = link.Value;
               Debug.Log("VALUE of " + currLink.Id + " " + currLink.ShortenedText);
               CreateLink(currLink);
            }

            //Show claims and hide rest of the tabs
            ToggleTabs("claim");
            ToggleType("claim");
            m_TypeManager.SetupTagButtons(responses);
            HideTabs();
        }

        // Reset a given response once used. If the response isn't placed in the chat,
        // delete will be true and the object can be freed from the pool before being
        // reallocated by CreateLink
        public void ResetLink(GameObject gameObject, string linkId, bool delete) 
        {
            RemoveResponse(gameObject);

            if (delete)
            {
                m_LinkPool.Free(gameObject.GetComponent<ChatBubble>());
            }

            CreateLink(m_Graph.FindLink(linkId));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_LinkContainer.transform);
        }

        // Helper function for removing a response from the responses list
        public void RemoveResponse(GameObject gameObject)
        {
            responses.Remove(gameObject);
        }

        public void ToggleType(string type) {
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

        // Allocate a new link from the pool and initialize its fields based on data from the graph
        private void CreateLink(Link link) 
        {
            ChatBubble newLink = m_LinkPool.Alloc(m_LinkContainer.transform);
            newLink.InitializeLinkDependencies(this, m_DropSlot, m_Graph);

            //TODO remove this check, temporary
            if(link.ShortenedText != null) {
                 newLink.InitializeLinkData(link.Id, link.Tag, link.Type, link.ShortenedText);
            } else {
                newLink.InitializeLinkData(link.Id, link.Tag, link.Type, link.DisplayText);
            }
            
            newLink.transform.SetSiblingIndex(link.Index);
            
            responses.Add(newLink.gameObject);
        }

        // Show responses with a given tag and hide all other responses
        private void ToggleTabs(string tagToShow) 
        {
            // foreach (GameObject gameObject in responses) 
            // {
            //     ChatBubble chatBubble = gameObject.GetComponent<ChatBubble>();

            //     if (chatBubble.linkTag.Equals(tagToShow)) 
            //     {
                   
            //         gameObject.SetActive(true);
            //     }
            //     else 
            //     {
            //         gameObject.SetActive(false);
            //     }
            // }

            m_TypeManager.ToggleButtons(tagToShow);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_LinkContainer.transform);
        }
        
        public void SelectClaim(string linkId) {
            currentClaim = linkId;
            ShowTabs();
            ToggleTabs("behavior");
            ToggleType("asdf");
        }        

        private void HideTabs() {
            m_TagButtons.SetActive(false);
        }

        private void ShowTabs() {
            m_TagButtons.SetActive(true);
        }
    }
}
