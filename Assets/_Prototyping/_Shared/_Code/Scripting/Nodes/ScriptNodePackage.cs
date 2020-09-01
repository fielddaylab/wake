using System;
using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;

namespace ProtoAqua
{
    public class ScriptNodePackage : IDataBlockPackage<ScriptNode>
    {
        private readonly Dictionary<string, ScriptNode> m_Nodes = new Dictionary<string, ScriptNode>(32, StringComparer.Ordinal);

        private string m_Name;
        [BlockMeta("basePath")] private string m_RootPath;

        public ScriptNodePackage(string inName)
        {
            m_Name = inName;
            m_RootPath = inName;
        }

        /// <summary>
        /// Name of this package.
        /// </summary>
        public string Name() { return m_Name; }

        /// <summary>
        /// Attempts to retrieve the node with the given id.
        /// </summary>
        public bool TryGetNode(string inId, out ScriptNode outNode)
        {
            return m_Nodes.TryGetValue(inId, out outNode);
        }

        /// <summary>
        /// Attempts to retrieve the entrypoint with the given id.
        /// </summary>
        public bool TryGetEntrypoint(string inId, out ScriptNode outNode)
        {
            ScriptNode node;
            if (m_Nodes.TryGetValue(inId, out node))
            {
                if ((node.Flags() & ScriptNodeFlags.Entrypoint) == ScriptNodeFlags.Entrypoint)
                {
                    outNode = node;
                    return true;
                }
            }

            outNode = null;
            return false;
        }

        /// <summary>
        /// Returns all entrypoints.
        /// </summary>
        public IEnumerable<ScriptNode> Entrypoints()
        {
            foreach(var node in m_Nodes.Values)
            {
                if ((node.Flags() & ScriptNodeFlags.Entrypoint) != 0)
                {
                    yield return node;
                }
            }
        }

        #region ICollection

        public int Count { get { return m_Nodes.Count; } }

        public IEnumerator<ScriptNode> GetEnumerator()
        {
            return m_Nodes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // ICollection

        #region Generator

        public class Generator : AbstractBlockGenerator<ScriptNode, ScriptNodePackage>
        {
            public override ScriptNodePackage CreatePackage(string inFileName)
            {
                return new ScriptNodePackage(inFileName);
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, ScriptNodePackage inPackage, TagData inId, out ScriptNode outBlock)
            {
                string selfId = inId.Id.ToString();
                inUtil.TempBuilder.Length = 0;
                inUtil.TempBuilder.Length = 0;
                inUtil.TempBuilder.Append(inPackage.m_RootPath);
                if (!inPackage.m_RootPath.EndsWith("."))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.Append(selfId);
                string fullId = inUtil.TempBuilder.Flush();
                outBlock = new ScriptNode(inPackage, selfId, fullId);
                inPackage.m_Nodes.Add(fullId, outBlock);
                return true;
            }
        }

        #endregion // Generator
    }
}