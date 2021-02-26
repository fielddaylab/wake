using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Portable;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ConceptMap : MonoBehaviour, IFactVisitor
    {
        #region Types

        [Serializable]
        public class NodePool : SerializablePool<ConceptMapNode> { }

        [Serializable]
        public class LinkPool : SerializablePool<ConceptMapLink> { }

        #endregion // Types

        #region Inspector

        [Header("Facts")]
        [SerializeField] private Button m_AddFactButton = null;

        [Header("Visualization")]
        [SerializeField] private RectTransform m_MapTransform = null;
        [SerializeField] private NodePool m_NodePool = null;
        [SerializeField] private LinkPool m_LinkPool = null;

        #endregion // Inspector

        private ConceptMapData m_MapData = new ConceptMapData();
        private HashSet<PlayerFactParams> m_AddedFacts = new HashSet<PlayerFactParams>();
        private Dictionary<StringHash32, ConceptMapNode> m_AllocatedNodes = new Dictionary<StringHash32, ConceptMapNode>();
        private Dictionary<StringHash32, ConceptMapLink> m_AllocatedLinks = new Dictionary<StringHash32, ConceptMapLink>();
        private Routine m_QueuedRebuild;

        #region Events

        private void Awake()
        {
            m_AddFactButton.onClick.AddListener(OnAddFactClick);
        }

        #endregion // Events

        #region Handlers

        private void OnAddFactClick()
        {
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (f) => !m_AddedFacts.Contains(f))
                .OnComplete((f) => {
                    AddFact(f);
                });
        }

        #endregion // Handlers

        #region Add/Remove

        public bool AddFact(PlayerFactParams inParams)
        {
            if (m_AddedFacts.Add(inParams))
            {
                if (!m_QueuedRebuild)
                    m_QueuedRebuild = Routine.StartCall(RebuildGraph);
                return true;
            }

            return false;
        }

        public bool RemoveFact(PlayerFactParams inParams)
        {
            if (m_AddedFacts.Remove(inParams))
            {
                if (!m_QueuedRebuild)
                    m_QueuedRebuild = Routine.StartCall(RebuildGraph);
                return true;
            }

            return false;
        }

        #endregion // Add/Remove

        #region Process

        private void RebuildGraph()
        {
            ProcessGraphData();
            ProcessGraphVisuals();
        }

        private void ProcessGraphData()
        {
            m_MapData.ClearAll();
            foreach(var factParams in m_AddedFacts)
            {
                factParams.Fact.Accept(this, factParams);
            }
        }

        private void ProcessGraphVisuals()
        {
            // TODO: Arrange these in some nice pattern?
            // will probably require some math and heuristics...

            using(PooledSet<StringHash32> unusedNames = PooledSet<StringHash32>.Create(m_AllocatedNodes.Keys))
            {
                ushort nodeCount = m_MapData.NodeCount();
                for(ushort i = 0; i < nodeCount; ++i)
                {
                    ref var nodeData = ref m_MapData.Node(i);
                    unusedNames.Remove(nodeData.Name);

                    ConceptMapNode visualNode;
                    if (!m_AllocatedNodes.TryGetValue(nodeData.Name, out visualNode))
                    {
                        visualNode = m_NodePool.Alloc();
                        m_AllocatedNodes.Add(nodeData.Name, visualNode);
                    }

                    float radius = visualNode.Radius() * 3f;
                    float rad = Mathf.PI * 2 * (float) i / nodeCount;
                    Vector2 pos = nodeCount == 1 ? Vector2.zero : new Vector2(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad));

                    visualNode.Load(pos, nodeData);
                }

                foreach(var name in unusedNames)
                {
                    var unusedNode = m_AllocatedNodes[name];
                    m_AllocatedNodes.Remove(name);
                    m_NodePool.Free(unusedNode);
                }

                unusedNames.Clear();

                foreach(var name in m_AllocatedLinks.Keys)
                    unusedNames.Add(name);

                ushort linkCount = m_MapData.LinkCount();
                for(ushort i = 0; i < linkCount; ++i)
                {
                    ref var linkData = ref m_MapData.Link(i);
                    unusedNames.Remove(linkData.Name);

                    ConceptMapLink visualLink;
                    if (!m_AllocatedLinks.TryGetValue(linkData.Name, out visualLink))
                    {
                        visualLink = m_LinkPool.Alloc();
                        m_AllocatedLinks.Add(linkData.Name, visualLink);
                    }

                    ConceptMapNode start = m_AllocatedNodes[m_MapData.Node(linkData.Start).Name];
                    ConceptMapNode end = m_AllocatedNodes[m_MapData.Node(linkData.End).Name];

                    visualLink.Load(start, end, linkData);
                }

                foreach(var name in unusedNames)
                {
                    var unusedLink = m_AllocatedLinks[name];
                    m_AllocatedLinks.Remove(name);
                    m_LinkPool.Free(unusedLink);
                }
            }
        }

        #endregion // Process

        #region IFactVisitor

        void IFactVisitor.Visit(BFBase inFact, PlayerFactParams inParams)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFBody inFact, PlayerFactParams inParams)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFWaterProperty inFact, PlayerFactParams inParams)
        {
            var waterPropDef = Services.Assets.WaterProp.Property(inFact.PropertyId());
            m_MapData.CreateNode(waterPropDef.Id(), "property", inFact);
        }

        void IFactVisitor.Visit(BFEat inFact, PlayerFactParams inParams)
        {
            ushort self = m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
            ushort target = m_MapData.CreateNode(inFact.Target().Id(), "critter", inFact.Target());
            m_MapData.CreateLink(inFact.Id(), self, target, "eat", inFact);
        }

        void IFactVisitor.Visit(BFGrow inFact, PlayerFactParams inParams)
        {
            m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFReproduce inFact, PlayerFactParams inParams)
        {
            m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFStateStarvation inFact, PlayerFactParams inParams)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFStateRange inFact, PlayerFactParams inParams)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFStateAge inFact, PlayerFactParams inParams)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        #endregion // IFactVisitor
    }
}