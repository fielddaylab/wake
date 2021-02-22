using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class ConceptMapData
    {
        public const ushort MaxNodes = 64;
        public const ushort MaxLinks = MaxNodes * 4;

        private readonly RingBuffer<ConceptMapNodeData> m_Nodes = new RingBuffer<ConceptMapNodeData>(MaxNodes);
        private readonly RingBuffer<ConceptMapLinkData> m_Links = new RingBuffer<ConceptMapLinkData>(MaxLinks);

        #region Clear

        public void ClearLinks()
        {
            m_Links.Clear();
        }

        public void ClearAll()
        {
            m_Links.Clear();
            m_Nodes.Clear();
        }

        #endregion // Clear

        #region Create

        public ushort CreateNode(StringHash32 inName, StringHash32 inType = default(StringHash32), object inTag = null)
        {
            ushort id;
            if (TryFindNode(inName, out id))
            {
                ref var node = ref m_Nodes[id];
                node.Type = inType;
                node.Tag = inTag;
                return id;
            }

            id = (ushort) m_Nodes.Count;
            m_Nodes.PushBack(new ConceptMapNodeData(inName, inType, inTag));
            return id;
        }

        public ushort CreateLink(StringHash32 inName, ushort inStart, ushort inEnd, StringHash32 inType = default(StringHash32), object inTag = null)
        {
            ushort id;
            if (TryFindLink(inName, out id))
            {
                ref var link = ref m_Links[id];
                link.Start = inStart;
                link.End = inEnd;
                link.Type = inType;
                link.Tag = inTag;
                return id;
            }

            id = (ushort) m_Links.Count;
            m_Links.PushBack(new ConceptMapLinkData(inName, inStart, inEnd, inType, inTag));
            return id;
        }
    
        #endregion // Create

        #region Access

        public ushort NodeCount() { return (ushort) m_Nodes.Count; }
        public ref ConceptMapNodeData Node(ushort inId) { return ref m_Nodes[inId]; }
        
        public bool TryFindNode(StringHash32 inName, out ushort outId)
        {
            for(int i = 0; i < m_Nodes.Count; ++i)
            {
                if (m_Nodes[i].Name == inName)
                {
                    outId = (ushort) i;
                    return true;
                }
            }

            outId = 0;
            return false;
        }

        public ushort LinkCount() { return (ushort) m_Links.Count; }
        public ref ConceptMapLinkData Link(ushort inId) { return ref m_Links[inId]; }

        public bool TryFindLink(StringHash32 inName, out ushort outId)
        {
            for(int i = 0; i < m_Links.Count; ++i)
            {
                if (m_Links[i].Name == inName)
                {
                    outId = (ushort) i;
                    return true;
                }
            }

            outId = 0;
            return false;
        }

        #endregion // Access
    }

    public struct ConceptMapNodeData
    {
        public StringHash32 Name;
        public StringHash32 Type;
        public object Tag;

        public ConceptMapNodeData(StringHash32 inName, StringHash32 inType, object inTag)
        {
            Name = inName;
            Type = inType;
            Tag = inTag;
        }
    }

    public struct ConceptMapLinkData
    {
        public StringHash32 Name;

        public ushort Start;
        public ushort End;

        public StringHash32 Type;
        public object Tag;

        public ConceptMapLinkData(StringHash32 inName, ushort inStart, ushort inEnd, StringHash32 inType, object inTag)
        {
            Name = inName;
            Start = inStart;
            End = inEnd;
            Type = inType;
            Tag = inTag;
        }
    }
}