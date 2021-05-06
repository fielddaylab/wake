using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    public class Graph : MonoBehaviour, ISceneLoadHandler, ISceneUnloadHandler
    {
        [Header("Graph Dependencies")]
        [SerializeField] private GraphDataManager m_GraphDataManager = null;

        #pragma warning disable CS0414

        [Header("-- DEBUG --")]
        [SerializeField] private GraphDataPackage m_DebugPackage = null;

        #pragma warning restore CS0414

        private Dictionary<StringHash32, Node> nodeDictionary = new Dictionary<StringHash32, Node>();
        private Dictionary<StringHash32, Link> linkDictionary = new Dictionary<StringHash32, Link>();

        private ConditionsData m_Conditions;
        private Node m_RootNode;
        private Node m_CurrentNode;
        [NonSerialized] private StringHash32 m_EndNodeId;
        [NonSerialized] private StringHash32 m_DefaultInvalidNodeId;
        [NonSerialized] private StringHash32 m_CharacterId;

        public event Action OnGraphLoaded;
        public event Action OnGraphNotAvailable;

        #region Accessors

        public Dictionary<StringHash32, Link> LinkDictionary
        {
            get { return linkDictionary; }
        }

        public Node RootNode
        {
            get { return m_RootNode; }
        }

        public StringHash32 EndNodeId
        {
            get { return m_EndNodeId; }
        }

        public ConditionsData Conditions
        {
            get { return m_Conditions; }
        }

        public StringHash32 CharacterId { get { return m_CharacterId; } }

        #endregion // Accessors

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext)
        {
            Services.Tweaks?.Unload(m_GraphDataManager);
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            Services.Tweaks.Load(m_GraphDataManager);

            JobDesc currentJob = Services.Data.CurrentJob()?.Job;
            GraphDataPackage script = currentJob?.FindAsset<GraphDataPackage>();
            
            #if UNITY_EDITOR
            if (!script && BootParams.BootedFromCurrentScene)
                script = m_DebugPackage;
            #else
            m_DebugPackage = null;
            #endif // UNITY_EDITOR

            if (script != null)
            {
                Services.Events.Dispatch(GameEvents.BeginArgument);
                LoadGraph(script);
            }
            else
            {
                if (OnGraphNotAvailable != null)
                    OnGraphNotAvailable();
            }
        }

        // Given a link id, check if that link is a valid response for the current node.
        // If valid, find that link and the id of the next node based on the current node.
        // Then check if conditions for traversing to that next node are met.
        public Node NextNode(StringHash32 id)
        {
            if (m_CurrentNode.CheckResponse(id))
            {
                StringHash32 nextNodeId = m_CurrentNode.GetNextNodeId(id);
                Node nextNode = FindNode(nextNodeId);

                if (nextNode != null)
                {
                    if (m_Conditions.CheckConditions(nextNode, id))
                    {
                        m_CurrentNode = nextNode;
                        return m_CurrentNode;
                    }
                    else
                    {
                        return m_CurrentNode = FindNode(m_CurrentNode.InvalidNodeId);
                    }
                }
                else
                {
                    // If no nextNodeId, go to default node
                    // TODO: Find better implementation for default node
                    return FindNode(m_CurrentNode.DefaultNodeId);
                }
            }
            else
            {
                // If id isn't valid, display invalid fact node
                if (m_CurrentNode.InvalidNodeId != null)
                {
                    return FindNode(m_CurrentNode.InvalidNodeId);
                }

                return FindNode(m_DefaultInvalidNodeId);
            }
        }

        public void SetCurrentNode(Node node) {
            m_CurrentNode = node;
        }

        // Helper method for finding a node given its id
        public Node FindNode(StringHash32 id)
        {
            if (nodeDictionary.TryGetValue(id, out Node node))
            {
                return node;
            }

            return null;
        }

        // Helper method for finding a link given its id
        public Link FindLink(StringHash32 id)
        {
            if (linkDictionary.TryGetValue(id, out Link link))
            {
                return link;
            }

            return null;
        }

        private void ResetGraph()
        {
            nodeDictionary = new Dictionary<StringHash32, Node>();
            linkDictionary = new Dictionary<StringHash32, Link>();
            m_RootNode = null;
            m_CurrentNode = null;
            m_EndNodeId = null;
            m_Conditions = null;
            m_CharacterId = null;
        }

        private void LoadGraph(GraphDataPackage inPackage)
        {
            ResetGraph();

            inPackage.Parse(Parsing.Block, new GraphDataPackage.Generator());

            foreach (KeyValuePair<StringHash32, Node> kvp in inPackage.Nodes)
            {
                Node node = kvp.Value;
                node.InitializeNode();
                nodeDictionary.Add(node.Id, node);
            }

            m_RootNode = FindNode(inPackage.RootNodeId);
            m_CharacterId = inPackage.CharacterId;

            // Checks if no root node was specified
            if (m_RootNode == null)
            {
                throw new System.ArgumentNullException("No root node specified");
            }

            m_CurrentNode = m_RootNode;
            m_Conditions = new ConditionsData(m_CurrentNode.Id);

            m_EndNodeId = inPackage.EndNodeId;

            if (m_EndNodeId == null)
            {
                throw new System.ArgumentNullException("No end node specified");
            }


            m_DefaultInvalidNodeId = inPackage.DefaultInvalidNodeId;

            if (m_DefaultInvalidNodeId == null)
            {
                throw new System.ArgumentNullException("No default invalid node specified");
            }

            if (!string.IsNullOrEmpty(inPackage.LinksFile))
            {
                LoadLinks(m_GraphDataManager.GetPackage(inPackage.LinksFile));
            }

            LoadLinks(inPackage);

            if (OnGraphLoaded != null)
                OnGraphLoaded();
        }

        private void LoadLinks(GraphDataPackage inPackage)
        {
            DataService dataService = Services.Data;

            foreach (KeyValuePair<StringHash32, Link> kvp in inPackage.Links)
            {
                Link link = kvp.Value;
                link.InitializeLink();
                linkDictionary.Add(link.Id, link);

            }

        }
    }
}
