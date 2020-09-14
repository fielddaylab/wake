using System;
using System.Collections.Generic;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation 
{
    public class LinkManager : MonoBehaviour 
    {
        [Serializable]
        public class LinkPool : SerializablePool<ChatBubble> { }
        
        [Header("Link Manager Dependencies")]
        [SerializeField] private Graph graph = null;
        [SerializeField] private GameObject linkPrefab = null;
        [SerializeField] private GameObject linkContainer = null;
        [SerializeField] private LinkPool m_LinkPool = null;

        [Header("Button Dependencies")]
        [SerializeField] private Button m_BehaviorsButton = null;
        [SerializeField] private Button m_EcosystemsButton = null;
        [SerializeField] private Button m_ModelsButton = null;

        private List<GameObject> responses = new List<GameObject>();

        private void Start() 
        {
            m_LinkPool.Initialize();

            m_BehaviorsButton.onClick.AddListener(() => ToggleTabs("behavior"));
            m_EcosystemsButton.onClick.AddListener(() => ToggleTabs("ecosystem"));
            m_ModelsButton.onClick.AddListener(() => ToggleTabs("model"));

            // Create links for each Link in the dictionary of the graph
            foreach (KeyValuePair<string, Link> link in graph.LinkDictionary) 
            {
               Link currLink = link.Value;
               CreateLink(currLink);
            }

            ToggleTabs("behavior");
        }

        public void ResetLink(GameObject gameObject, string linkId) 
        {
            responses.Remove(gameObject);
            CreateLink(graph.FindLink(linkId));
        }

        private void CreateLink(Link link) 
        {
            // Create the link and get its Transform
            ChatBubble newLink = m_LinkPool.Alloc(linkContainer.transform);
            Transform newLinkTransform = newLink.transform;
            GameObject newLinkGameObject = newLink.gameObject;
            
            newLinkTransform.SetSiblingIndex(link.Index);

            // Create the link from prefab and adjust its properties
            newLink.GetComponent<ChatBubble>().bubbleType = BubbleType.Link;
            newLinkTransform.GetComponent<ChatBubble>().id = link.Id;
            newLinkTransform.GetComponent<ChatBubble>().linkTag = link.Tag;
            newLinkTransform.Find("LinkText").GetComponent<TextMeshProUGUI>().SetText(link.DisplayText);

            responses.Add(newLinkGameObject);
        }

        private void ToggleTabs(string tagToShow) 
        {
            foreach (GameObject gameObject in responses) 
            {
                ChatBubble chatBubble = gameObject.GetComponent<ChatBubble>();

                if (chatBubble.linkTag.Equals(tagToShow)) 
                {
                    gameObject.SetActive(true);
                }
                else 
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
