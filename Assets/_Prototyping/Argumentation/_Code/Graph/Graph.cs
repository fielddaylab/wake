using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    public class Graph : MonoBehaviour
    {
        [Header("Graph Dependencies")]
        [SerializeField] private GraphDataManager m_GraphDataManager = null;

        private Dictionary<string, Node> nodeDictionary = new Dictionary<string, Node>();
        private Dictionary<string, Link> linkDictionary = new Dictionary<string, Link>();

        private ConditionsData conditions;
        private Node rootNode;
        private Node currentNode;
        private string endNodeId;

        #region Accessors

        public Dictionary<string, Link> LinkDictionary
        {
            get { return linkDictionary; }
        }

        public Node RootNode
        {
            get { return rootNode; }
        }

        public string EndNodeId
        {
            get { return endNodeId; }
        }

        public ConditionsData Conditions
        {
            get { return conditions; }
        }

        #endregion // Accessors

        // Load graph data, create nodes and links
        private void Awake()
        {
            Services.Tweaks.Load(m_GraphDataManager);
            GraphDataPackage data = m_GraphDataManager.MasterPackage;

            foreach (KeyValuePair<string, Node> kvp in data.Nodes)
            {
                Node node = kvp.Value;
                node.InitializeNode();
                nodeDictionary.Add(node.Id, node);
            }

            rootNode = FindNode(data.RootNodeId);

            // Checks if no root node was specified
            if (rootNode == null)
            {
                throw new System.ArgumentNullException("No root node specified");
            }

            currentNode = rootNode;
            conditions = new ConditionsData(currentNode.Id);

            endNodeId = data.EndNodeId;

            if (endNodeId == null)
            {
                throw new System.ArgumentNullException("No end node specified");
            }
            
            foreach (KeyValuePair<string, Link> kvp in data.Links)
            {
                Link link = kvp.Value;
                link.InitializeLink();
                linkDictionary.Add(link.Id, link);
            }
        }

        // Helper method for finding a node given its id
        public Node FindNode(string id)
        {
            if (nodeDictionary.TryGetValue(id, out Node node))
            {
                return node;
            }

            return null;
        }

        // Helper method for finding a link given its id
        public Link FindLink(string id)
        {
            if (linkDictionary.TryGetValue(id, out Link link))
            {
                return link;
            }

            return null;
        }

        // Given a link id, check if that link is a valid response for the current node.
        // If valid, find that link and the id of the next node based on the current node.
        // Then check if conditions for traversing to that next node are met.
        public Node NextNode(string id)
        {
            bool validFact = currentNode.CheckResponse(id);

            if (validFact)
            {
                Link response = FindLink(id);
                string nextNodeId = response.GetNextNodeId(currentNode.Id);
                Node nextNode = FindNode(nextNodeId);

                if (nextNode != null)
                {
                    if (conditions.CheckConditions(nextNode, response))
                    {
                        currentNode = nextNode;
                        return currentNode;
                    }
                    else
                    {
                        // If CheckConditions returns false, cycle back to conditions not met node
                        Node conditionsNotMetNode = FindNode(response.ConditionsNotMetId);
                        currentNode = conditionsNotMetNode;
                        return currentNode;
                    }
                }
                else
                {
                    // If no nextNodeId, go to default node
                    // TODO: Find better implementation for default node
                    Node defaultNode = FindNode(currentNode.DefaultNodeId);
                    return defaultNode;
                }
            }
            else
            {
                // If id isn't valid, display invalid fact node
                Debug.Log("Invalid!");
                Node invalidFactNode = FindNode(currentNode.InvalidNodeId);
                return invalidFactNode;
            }
        }
    }
}
