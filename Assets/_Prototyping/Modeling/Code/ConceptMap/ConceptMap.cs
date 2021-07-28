using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProtoAqua.Modeling
{
    public class ConceptMap : MonoBehaviour, IFactVisitor, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Types

        [Serializable]
        public class NodePool : SerializablePool<ConceptMapNode> { }

        [Serializable]
        public class LinkPool : SerializablePool<ConceptMapLink> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private RectTransform m_MapTransform = null;
        [SerializeField] private RectTransform m_ContentTransform = null;
        [SerializeField] private RectTransform m_EmptyTransform = null;

        [Header("Visualization")]
        [SerializeField] private NodePool m_NodePool = null;
        [SerializeField] private LinkPool m_LinkPool = null;
        [SerializeField] private float m_NodeSpacingRatio = 2.5f;
        [SerializeField] private Texture2D m_SolidLineTexture = null;
        [SerializeField] private Texture2D m_DottedLineTexture = null;

        [Header("Input")]
        [SerializeField] private Vector2 m_ContentSizeBuffer = new Vector2(64, 64);
        [SerializeField] private float m_LerpFactor = 10;

        #endregion // Inspector

        private ConceptMapData m_MapData = new ConceptMapData();
        private HashSet<BFBase> m_AddedFacts = new HashSet<BFBase>();
        private Dictionary<StringHash32, ConceptMapNode> m_AllocatedNodes = new Dictionary<StringHash32, ConceptMapNode>();
        private Dictionary<StringHash32, ConceptMapLink> m_AllocatedLinks = new Dictionary<StringHash32, ConceptMapLink>();
        private Routine m_QueuedRebuild;

        private Action<object> m_CachedLinkClicked;
        private Action<object> m_CachedNodeClicked;

        [NonSerialized] private Rect m_ContentBounds;
        [NonSerialized] private Vector2 m_DragPointerStart;
        [NonSerialized] private Vector2 m_DragContentStart;
        [NonSerialized] private bool m_Dragging;

        public Action<BestiaryDesc> OnNodeRequestToggle;

        public IReadOnlyCollection<ConceptMapNode> Nodes()
        {
            return m_AllocatedNodes.Values;
        }

        public ConceptMapNode Node(StringHash32 inId)
        {
            return m_AllocatedNodes[inId];
        }

        #region Handlers

        private void Awake()
        {
            RebuildGraph();
        }

        private void OnDisable()
        {
            m_Dragging = false;
        }

        private void OnLinkClicked(object inTag)
        {
            // if (m_Locked)
            //     return;
        }

        private void OnNodeClicked(object inTag)
        {
            if (OnNodeRequestToggle == null)
                return;

            BestiaryDesc desc = inTag as BestiaryDesc;
            if (desc != null)
            {
                OnNodeRequestToggle(desc);
            }
        }

        private void LateUpdate()
        {
            // TODO: Implement... better
            if (m_Dragging)
                return;
            
            Vector2 contentOffset = m_ContentTransform.anchoredPosition + m_ContentBounds.center;
            Rect fullRect = m_MapTransform.rect;
            Vector2 targetOffset = Geom.Constrain(contentOffset, m_ContentSizeBuffer, fullRect);
            
            float lerpFactor = TweenUtil.Lerp(m_LerpFactor);
            Vector2 newOffset = Vector2.LerpUnclamped(contentOffset, targetOffset, lerpFactor);
            m_ContentTransform.anchoredPosition = newOffset - m_ContentBounds.center;
        }

        #endregion // Handlers

        #region Add/Remove

        public bool AddFact(BFBase inParams)
        {
            if (m_AddedFacts.Add(inParams))
            {
                if (!m_QueuedRebuild)
                    m_QueuedRebuild = Routine.StartCall(RebuildGraph);
                return true;
            }

            return false;
        }

        public bool RemoveFact(BFBase inParams)
        {
            if (m_AddedFacts.Remove(inParams))
            {
                if (!m_QueuedRebuild)
                    m_QueuedRebuild = Routine.StartCall(RebuildGraph);
                return true;
            }

            return false;
        }

        public bool ClearFacts()
        {
            if (m_AddedFacts.Count > 0)
            {
                m_AddedFacts.Clear();
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
            ProcessGraphToggles();
            ProcessGraphVisuals();
        }

        private void ProcessGraphData()
        {
            m_MapData.ClearAll();
            foreach(var fact in m_AddedFacts)
            {
                fact.Accept(this);
            }
        }

        private void ProcessGraphToggles()
        {
            if (m_MapData.NodeCount() > 0)
            {
                m_MapTransform.gameObject.SetActive(true);
                m_EmptyTransform.gameObject.SetActive(false);
            }
            else
            {
                m_MapTransform.gameObject.SetActive(false);
                m_EmptyTransform.gameObject.SetActive(true);
            }
        }

        private void ProcessGraphVisuals()
        {
            // TODO: Arrange these in some nice pattern?
            // will probably require some math and heuristics...

            m_ContentBounds = default(Rect);

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

                    float radius = visualNode.Radius() * m_NodeSpacingRatio;
                    float rad = Mathf.PI * 2 * (float) i / nodeCount;
                    Vector2 pos = nodeCount == 1 ? Vector2.zero : new Vector2(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad));

                    visualNode.OnClick = m_CachedNodeClicked ?? (m_CachedNodeClicked = OnNodeClicked);
                    visualNode.Load(pos, nodeData);

                    Geom.Encapsulate(ref m_ContentBounds, new Rect(pos.x - radius, pos.y - radius, radius * 2, radius * 2));
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

                    Texture2D lineTexture = m_SolidLineTexture;
                    
                    BFBase fact = linkData.Tag as BFBase;
                    if (fact != null && !Services.Data.Profile.Bestiary.IsFactFullyUpgraded(fact.Id()))
                    {
                        lineTexture = m_DottedLineTexture;
                    }

                    visualLink.OnClick = m_CachedLinkClicked ?? (m_CachedLinkClicked = OnLinkClicked);
                    visualLink.Load(start, end, linkData, lineTexture);
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

        void IFactVisitor.Visit(BFBase inFact)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFBody inFact)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFWaterProperty inFact)
        {
            // var waterPropDef = Services.Assets.WaterProp.Property(inFact.PropertyId());
            // m_MapData.CreateNode(waterPropDef.Id(), "property", inFact);
        }

        void IFactVisitor.Visit(BFPopulation inFact)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFEat inFact)
        {
            ushort self = m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
            ushort target = m_MapData.CreateNode(inFact.Target().Id(), "critter", inFact.Target());
            m_MapData.CreateLink(inFact.Id(), self, target, "eat", inFact);
        }

        void IFactVisitor.Visit(BFGrow inFact)
        {
            m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFReproduce inFact)
        {
            m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFProduce inFact)
        {
            ushort self = m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
            var propertyDef = Services.Assets.WaterProp.Property(inFact.Target());
            ushort target = m_MapData.CreateNode(propertyDef.Id(), "property", propertyDef);
            m_MapData.CreateLink(inFact.Id(), self, target, "produce", inFact);
        }

        void IFactVisitor.Visit(BFConsume inFact)
        {
            ushort self = m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
            var propertyDef = Services.Assets.WaterProp.Property(inFact.Target());
            ushort target = m_MapData.CreateNode(propertyDef.Id(), "property", propertyDef);
            m_MapData.CreateLink(inFact.Id(), self, target, "consume", inFact);
        }

        void IFactVisitor.Visit(BFState inFact)
        {
            ushort self = m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
            var propertyDef = Services.Assets.WaterProp.Property(inFact.PropertyId());
            ushort target = m_MapData.CreateNode(propertyDef.Id(), "property", propertyDef);
            m_MapData.CreateLink(inFact.Id(), self, target, "range", inFact);
        }

        void IFactVisitor.Visit(BFDeath inFact)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        void IFactVisitor.Visit(BFModel inFact)
        {
            // m_MapData.CreateNode(inFact.Parent().Id(), "critter", inFact.Parent());
        }

        #endregion // IFactVisitor

        #region IDragHandler

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (m_MapData.NodeCount() == 0 || !isActiveAndEnabled || eventData.button != 0)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MapTransform, eventData.position, eventData.pressEventCamera, out m_DragPointerStart);
            m_DragContentStart = m_ContentTransform.anchoredPosition;
            m_Dragging = true;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging || eventData.button != 0)
                return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MapTransform, eventData.position, eventData.pressEventCamera, out localPoint);
            Vector2 delta = localPoint - m_DragPointerStart;
            Vector2 newPos = m_DragContentStart + delta;
            m_ContentTransform.anchoredPosition = newPos;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!m_Dragging || eventData.button != 0)
                return;

            m_Dragging = false;
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            // TODO: What?
        }

        #endregion // IDragHandler
    }
}