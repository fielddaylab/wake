using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil.Blocks;
using BeauUtil.Tags;

namespace ProtoAqua.Argumentation
{
    public class GraphDataPackage : ScriptableDataBlockPackage<GraphData>
    {
        [NonSerialized] private readonly Dictionary<string, GraphData> m_Data = new Dictionary<string, GraphData>(32, StringComparer.Ordinal);
        [NonSerialized] private readonly Dictionary<string, Node> m_Nodes = new Dictionary<string, Node>(32, StringComparer.Ordinal);
        [NonSerialized] private readonly Dictionary<string, Link> m_Links = new Dictionary<string, Link>(32, StringComparer.Ordinal);

        // Package Ids
        [BlockMeta("rootNodeId")] private string m_RootNodeId = null;
        [BlockMeta("endNodeId")] private string m_EndNodeId = null;
        [BlockMeta("defaultInvalidNodeId")] private string m_DefaultInvalidNodeId = null;

        #region Accessors

        public Dictionary<string, Node> Nodes
        {
            get { return m_Nodes; }
        }

        public Dictionary<string, Link> Links
        {
            get { return m_Links; }
        }

        public string RootNodeId
        { 
            get { return m_RootNodeId; }
        }

        public string EndNodeId
        { 
            get { return m_EndNodeId; }
        }

        public string DefaultInvalidNodeId
        {
            get { return m_DefaultInvalidNodeId; }
        }

        #endregion // Accessors

        #region ICollection

        public override int Count { get { return m_Data.Count; } }

        public override IEnumerator<GraphData> GetEnumerator()
        {
            return m_Data.Values.GetEnumerator();
        }

        public override void Clear()
        {
            m_Data.Clear();
            m_Links.Clear();
            m_Nodes.Clear();
        }

        #endregion // ICollection

        #region Importer

        #if UNITY_EDITOR

        [ScriptedExtension(1, "argue")]
        private class Importer : ImporterBase<GraphDataPackage>
        {
        }

        #endif // UNITY_EDITOR

        #endregion // Importer

        #region Generator

        public class Generator : GeneratorBase<GraphDataPackage>
        {
            public override bool TryCreateBlock(IBlockParserUtil inUtil, GraphDataPackage inPackage, TagData inId, out GraphData outBlock)
            {
                string id = inId.Id.ToString();

                if (id.StartsWith("node"))
                {
                    outBlock = new Node(id);
                    inPackage.m_Data.Add(id, outBlock);
                    inPackage.m_Nodes.Add(id, (Node)outBlock);
                    return true;
                } 
                else //@TODO FIX
                {
                    outBlock = new Link(id);
                    inPackage.m_Data.Add(id, outBlock);
                    inPackage.m_Links.Add(id, (Link)outBlock);
                    return true;
                } 
                // else
                // {
                //     throw new System.ArgumentException("Invalid id format");
                // }
            }
        }

        #endregion // Generator
    }
}
