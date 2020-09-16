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
            foreach (KeyValuePair<string, Link> link in m_Graph.LinkDictionary) 
            {
               Link currLink = link.Value;
               CreateLink(currLink);
            }

            ToggleTabs("behavior");
        }

        public void ResetLink(GameObject gameObject, string linkId, bool delete) 
        {
            responses.Remove(gameObject);

            if (delete)
            {
                m_LinkPool.Free(gameObject.GetComponent<ChatBubble>());
            }

            CreateLink(m_Graph.FindLink(linkId));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_LinkContainer.transform);
        }

        private void CreateLink(Link link) 
        {
            ChatBubble newLink = m_LinkPool.Alloc(m_LinkContainer.transform);
            newLink.InitializeLinkDependencies(this, m_DropSlot);
            newLink.InitializeLinkData(link.Id, link.Tag, link.DisplayText);
            
            newLink.transform.SetSiblingIndex(link.Index);
            
            GameObject newLinkGameObject = newLink.gameObject;
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

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m_LinkContainer.transform);
        }
    }
}
