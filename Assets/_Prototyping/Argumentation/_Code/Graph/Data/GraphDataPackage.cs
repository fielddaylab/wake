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
        private readonly Dictionary<string, NodeData> m_Nodes = new Dictionary<string, NodeData>(32, StringComparer.Ordinal);
        private readonly Dictionary<string, LinkData> m_Links = new Dictionary<string, LinkData>(32, StringComparer.Ordinal);

        private string m_Name;

        public GraphDataPackage(string inName)
        {
            m_Name = inName;
        }

        #region Accessors

        public Dictionary<string, NodeData> Nodes
        {
            get { return m_Nodes; }
        }

        public Dictionary<string, LinkData> Links
        {
            get { return m_Links; }
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
                    outBlock = new NodeData(id);
                    inPackage.m_Data.Add(id, outBlock);
                    inPackage.m_Nodes.Add(id, (NodeData)outBlock);
                    return true;
                } else if (id.StartsWith("link"))
                {
                    outBlock = new LinkData(id);
                    inPackage.m_Data.Add(id, outBlock);
                    inPackage.m_Links.Add(id, (LinkData)outBlock);
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
