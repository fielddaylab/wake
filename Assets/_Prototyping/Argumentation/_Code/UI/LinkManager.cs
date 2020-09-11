using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation {

    public class LinkManager : MonoBehaviour {
        
        [SerializeField] Graph graph = null;
        [SerializeField] GameObject linkPrefab = null;
        [SerializeField] GameObject linkContainer = null;

        [Header("Button Dependencies")]
        [SerializeField] private Button m_BehaviorsButton = null;
        [SerializeField] private Button m_EcosystemsButton = null;
        [SerializeField] private Button m_ModelsButton = null;

        private List<GameObject> responses = new List<GameObject>();

        // Start is called before the first frame update
        void Start() {
            m_BehaviorsButton.onClick.AddListener(() => ToggleTabs("behavior"));
            m_EcosystemsButton.onClick.AddListener(() => ToggleTabs("ecosystem"));
            m_ModelsButton.onClick.AddListener(() => ToggleTabs("model"));

            //Create links for each Link in the dictionary of the graph
           foreach(KeyValuePair<string, Link> link in graph.LinkDictionary) {
               Link currLink = link.Value;
               CreateLink(currLink);
           }

           ToggleTabs("behavior");
        }

        private void CreateLink(Link link) {
            //Create the link and get its Transform
            GameObject newLink = Instantiate(linkPrefab, linkContainer.transform);
            Transform newLinkTransform = newLink.transform;
            
            newLinkTransform.SetSiblingIndex(link.Index);

            //Create the link from prefab and adjust its properties
            newLink.GetComponent<ChatBubble>().bubbleType = BubbleType.Link;
            newLinkTransform.GetComponent<ChatBubble>().id = link.Id;
            newLinkTransform.GetComponent<ChatBubble>().linkTag = link.Tag;
            newLinkTransform.Find("LinkText").GetComponent<TextMeshProUGUI>().SetText(link.DisplayText);

            newLink.SetActive(true);

            responses.Add(newLink);
        }

        private void ToggleTabs(string tagToShow) {
            foreach (GameObject gameObject in responses) {
                ChatBubble chatBubble = gameObject.GetComponent<ChatBubble>();

                if (chatBubble.linkTag.Equals(tagToShow)) {
                    gameObject.SetActive(true);
                }
                else {
                    gameObject.SetActive(false);
                }
            }
        }

        public void ResetLink(GameObject gameObject, string linkId, bool delete) {
            responses.Remove(gameObject);
            if(delete) {
                Destroy(gameObject);
            }
            CreateLink(graph.FindLink(linkId));
        }



    }
}
