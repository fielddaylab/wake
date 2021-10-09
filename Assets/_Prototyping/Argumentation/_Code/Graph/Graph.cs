#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    public class Graph : MonoBehaviour, ISceneLoadHandler
    {
        #pragma warning disable CS0414

        [Header("-- DEBUG --")]
        [SerializeField] private GraphDataPackage m_DebugPackage = null;

        #pragma warning restore CS0414

        private Dictionary<StringHash32, ArgueNode> m_NodeMap;
        private Dictionary<StringHash32, ArgueLink> m_LinkMap;
        [NonSerialized] private StringHash32 m_RootNodeId;
        [NonSerialized] private StringHash32 m_EndNodeId;
        [NonSerialized] private StringHash32 m_DefaultInvalidNodeId;
        [NonSerialized] private StringHash32 m_CharacterId;

        public event Action OnGraphLoaded;
        public event Action OnGraphNotAvailable;

        #region Accessors

        public StringHash32 RootNodeId {
            get { return m_RootNodeId; }
        }
        public StringHash32 EndNodeId {
            get { return m_EndNodeId; }
        }
        public StringHash32 CharacterId {
            get { return m_CharacterId; }
        }
        public IEnumerable<ArgueLink> Links {
            get { return m_LinkMap.Values; }
        }

        #endregion // Accessors

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
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
        public ArgueNode NextNode(ArgueNode current, StringHash32 linkId) {
            StringHash32 nextNodeId = EvaluateNextNodeId(current, linkId);
            if (!nextNodeId.IsEmpty)
            {
                ArgueNode nextNode = FindNode(nextNodeId);
                if (nextNode != null) {
                    return nextNode;
                }
            }
            if (current.InvalidNodeId != null) {
                return FindNode(current.InvalidNodeId);
            }

            return FindNode(m_DefaultInvalidNodeId);
        }

        public ArgueNode NextNode(ArgueNode current) {
            var potentialNexts = current.PotentialNexts;
            for(int i = 0; i < potentialNexts.Length; i++) {
                ArgueNode.PotentialNext entry = potentialNexts[i];
                if (!string.IsNullOrEmpty(entry.Conditions) && !Services.Data.CheckConditions(entry.Conditions)) {
                    continue;
                }
                return FindNode(entry.NodeId);
            }

            return null;
        }

        private StringHash32 EvaluateNextNodeId(ArgueNode inCurrent, StringHash32 inLinkId) {
            var potentialLinks = inCurrent.PotentialLinks;
            for(int i = 0; i < potentialLinks.Length; i++) {
                ArgueNode.PotentialLink entry = potentialLinks[i];
                if (entry.LinkId != inLinkId) {
                    continue;
                }
                if (!string.IsNullOrEmpty(entry.Conditions) && !Services.Data.CheckConditions(entry.Conditions)) {
                    continue;
                }
                return entry.NodeId;
            }
            
            return null;
        }

        // Helper method for finding a node given its id
        public ArgueNode FindNode(StringHash32 id) {
            m_NodeMap.TryGetValue(id, out ArgueNode node);
            return node;
        }

        // Helper method for finding a link given its id
        public ArgueLink FindLink(StringHash32 id) {
            m_LinkMap.TryGetValue(id, out ArgueLink link);
            return link;
        }

        private void ResetGraph() {
            m_NodeMap = null;
            m_LinkMap = null;
            m_RootNodeId = null;
            m_EndNodeId = null;
            m_CharacterId = null;
        }

        private void LoadGraph(GraphDataPackage inPackage) {
            ResetGraph();

            inPackage.Parse(new GraphDataPackage.Generator());

            m_NodeMap = inPackage.Nodes;
            m_LinkMap = inPackage.Links;
            m_CharacterId = inPackage.CharacterId;
            m_RootNodeId = inPackage.RootNodeId;
            m_EndNodeId = inPackage.EndNodeId;
            m_DefaultInvalidNodeId = inPackage.DefaultInvalidNodeId;

            #if DEVELOPMENT
            using(Profiling.Time("validating argumentation graph")) {
                ValidateGraph();
            }
            #endif // DEVELOPMENT

            if (OnGraphLoaded != null)
                OnGraphLoaded();
        }

        #if DEVELOPMENT
        private void ValidateGraph() {
            Assert.False(m_RootNodeId.IsEmpty, "No root node specified for graph");
            Assert.False(m_EndNodeId.IsEmpty, "No end node specified for graph");
            Assert.False(m_DefaultInvalidNodeId.IsEmpty, "No default invalid node specified for graph");

            Assert.True(m_NodeMap.ContainsKey(m_RootNodeId), "Root node '{0}' is missing", m_RootNodeId);
            Assert.True(m_NodeMap.ContainsKey(m_EndNodeId), "End node '{0}' is missing", m_EndNodeId);
            Assert.True(m_NodeMap.ContainsKey(m_DefaultInvalidNodeId), "Default invalid node '{0}' is missing", m_DefaultInvalidNodeId);

            foreach(var node in m_NodeMap.Values) {
                StringHash32 invalid = node.InvalidNodeId;
                if (!invalid.IsEmpty) {
                    Assert.True(m_NodeMap.ContainsKey(invalid), "Node '{0}' links to missing invalid node '{1}'", node.Id, invalid);
                }

                foreach(var link in node.PotentialLinks) {
                    Assert.True(m_NodeMap.ContainsKey(link.NodeId), "Node '{0}' links to missing response node '{1}'", node.Id, link.NodeId);
                }

                foreach(var next in node.PotentialNexts) {
                    Assert.True(m_NodeMap.ContainsKey(next.NodeId), "Node '{0}' links to missing next node '{1}'", node.Id, next.NodeId);
                }

                if (node.Id != m_EndNodeId && !node.CancelFlow && node.PotentialLinks.IsEmpty && node.PotentialNexts.IsEmpty) {
                    Log.Warn("[Graph] Node '{0}' has no links and no next nodes", node.Id);
                }
            }
        }
        #endif // DEVELOPMENT
    }
}
