using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;

namespace ProtoAqua.Argumentation
{
    public class GraphDataPackage : ScriptableDataBlockPackage<GraphData>
    {
        [NonSerialized] private readonly Dictionary<StringHash32, GraphData> m_Data = new Dictionary<StringHash32, GraphData>(32);
        [NonSerialized] private readonly Dictionary<StringHash32, Node> m_Nodes = new Dictionary<StringHash32, Node>(32);
        [NonSerialized] private readonly Dictionary<StringHash32, Link> m_Links = new Dictionary<StringHash32, Link>(32);

        // Package Ids
        [BlockMeta("rootNodeId")] private StringHash32 m_RootNodeId = null;
        [BlockMeta("endNodeId")] private StringHash32 m_EndNodeId = null;
        [BlockMeta("defaultInvalidNodeId")] private StringHash32 m_DefaultInvalidNodeId = null;
        [BlockMeta("linksFile")] private string m_LinksFile = null;
        [BlockMeta("characterId")] private StringHash32 m_CharacterId = null;

        #region Accessors

        public Dictionary<StringHash32, Node> Nodes { get { return m_Nodes; } }
        public Dictionary<StringHash32, Link> Links { get { return m_Links; } }

        public StringHash32 RootNodeId { get { return m_RootNodeId; } }
        public StringHash32 EndNodeId {  get { return m_EndNodeId; } }
        public StringHash32 DefaultInvalidNodeId { get { return m_DefaultInvalidNodeId; } }
        public StringHash32 CharacterId { get { return m_CharacterId; } }
        
        public string LinksFile
        {
            get { return m_LinksFile; }
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
        private class Importer : ImporterBase<GraphDataPackage> { }

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
