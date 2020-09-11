using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil.Blocks;
using BeauUtil.Tags;

namespace ProtoAqua.Argumentation
{
    public class GraphDataPackage : IDataBlockPackage<GraphData>
    {
        private readonly Dictionary<string, GraphData> m_Data = new Dictionary<string, GraphData>(32, StringComparer.Ordinal);
        private readonly Dictionary<string, Node> m_Nodes = new Dictionary<string, Node>(32, StringComparer.Ordinal);
        private readonly Dictionary<string, Link> m_Links = new Dictionary<string, Link>(32, StringComparer.Ordinal);

        private string m_Name;

        public GraphDataPackage(string inName)
        {
            m_Name = inName;
        }

        // Package Ids
        [BlockMeta("rootNodeId")] private string m_RootNodeId = null;
        [BlockMeta("endNodeId")] private string m_EndNodeId = null;

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

        #endregion // Accessors

        #region ICollection

        public int Count { get { return m_Data.Count; } }

        public IEnumerator<GraphData> GetEnumerator()
        {
            return m_Data.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // ICollection

        #region Generator

        public class Generator : AbstractBlockGenerator<GraphData, GraphDataPackage>
        {
            public override GraphDataPackage CreatePackage(string inFileName)
            {
                return new GraphDataPackage(inFileName);
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, 
            GraphDataPackage inPackage, TagData inId, out GraphData outBlock)
            {
                string id = inId.Id.ToString();

                if (id.StartsWith("node"))
                {
                    outBlock = new Node(id);
                    inPackage.m_Data.Add(id, outBlock);
                    inPackage.m_Nodes.Add(id, (Node)outBlock);
                    return true;
                } else if (id.StartsWith("link"))
                {
                    outBlock = new Link(id);
                    inPackage.m_Data.Add(id, outBlock);
                    inPackage.m_Links.Add(id, (Link)outBlock);
                    return true;
                } else
                {
                    throw new System.ArgumentException("Invalid id format");
                }
            }
        }

        #endregion // Generator
    }
}
