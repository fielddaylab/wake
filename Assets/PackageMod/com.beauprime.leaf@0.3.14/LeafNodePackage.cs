/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafNodePackage.cs
 * Purpose: Package of leaf nodes.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using Leaf.Compiler;
using Leaf.Runtime;

namespace Leaf
{
    public abstract class LeafNodePackage : IDataBlockPackage<LeafNode>
    {
        public string Name() { return m_Name; }
        public string RootPath() { return m_RootPath; }

        protected string m_Name;
        [BlockMeta("basePath"), UnityEngine.Scripting.Preserve] protected string m_RootPath = string.Empty;

        protected readonly Dictionary<StringHash32, string> m_LineTable = new Dictionary<StringHash32, string>(CompareUtils.DefaultEquals<StringHash32>());
        protected readonly Dictionary<StringHash32, string> m_LineNames = new Dictionary<StringHash32, string>(CompareUtils.DefaultEquals<StringHash32>());
        protected internal LeafInstructionBlock m_Instructions;

        internal LeafCompiler m_Compiler;
        internal LeafCompiler.Report m_ErrorState;

        internal void SetLines(Dictionary<StringHash32, string> inLineTable, Dictionary<StringHash32, string> inLineNameTable)
        {
            m_LineTable.Clear();
            m_LineTable.EnsureCapacity(inLineTable.Count);
            foreach (var kv in inLineTable)
            {
                m_LineTable.Add(kv.Key, kv.Value);
            }

            m_LineNames.Clear();
            m_LineNames.EnsureCapacity(inLineNameTable.Count);
            foreach(var kv in inLineNameTable)
            {
                m_LineNames.Add(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Attempts to retrieve the line with the specific line code in this package.
        /// </summary>
        public bool TryGetLine(StringHash32 inLineCode, out string outLine)
        {
            return m_LineTable.TryGetValue(inLineCode, out outLine);
        }

        /// <summary>
        /// Retrieves the custom name associated with the given linecode.
        /// </summary>
        public string GetLineCustomName(StringHash32 inLineCode)
        {
            string meta;
            m_LineNames.TryGetValue(inLineCode, out meta);
            return meta;
        }

        /// <summary>
        /// Returns all lines embedded in this package.
        /// </summary>
        public IEnumerable<KeyValuePair<StringHash32, string>> AllLines()
        {
            return m_LineTable;
        }

        /// <summary>
        /// Gathers all lines in this package that have a custom name associated with it.
        /// </summary>
        public int GatherAllLinesWithCustomNames(ICollection<KeyValuePair<StringHash32, string>> outLines)
        {
            if (outLines == null)
                throw new ArgumentNullException("outLines");

            int count = 0;
            foreach(var key in m_LineTable.Keys)
            {
                if (m_LineNames.TryGetValue(key, out string customName))
                {
                    outLines.Add(new KeyValuePair<StringHash32, string>(key, customName));
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Attempts to retrieve the node with the specific id in this package.
        /// </summary>
        public abstract bool TryGetNode(StringHash32 inNodeId, out LeafNode outNode);

        /// <summary>
        /// Clears this package.
        /// </summary>
        public virtual void Clear()
        {
            m_LineTable.Clear();
            m_LineNames.Clear();
            m_Instructions = default(LeafInstructionBlock);
        }

        /// <summary>
        /// Returns the compilation error state.
        /// </summary>
        public LeafCompiler.Report ErrorState()
        {
            return m_ErrorState;
        }

        #region IDataBlockPackage

        public abstract int Count { get; }
        public abstract IEnumerator<LeafNode> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<LeafNode> IEnumerable<LeafNode>.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // IDataBlockPackage
    }

    public class LeafNodePackage<TNode> : LeafNodePackage, IDataBlockPackage<TNode>
        where TNode : LeafNode
    {
        // temp storage
        protected readonly Dictionary<StringHash32, TNode> m_Nodes = new Dictionary<StringHash32, TNode>(32, CompareUtils.DefaultEquals<StringHash32>());

        public LeafNodePackage(string inName)
        {
            m_Name = inName;
        }

        public override int Count { get { return m_Nodes.Count; } }

        #region Modifications

        internal void AddNode(TNode inNode)
        {
            StringHash32 nodeId = inNode.Id();
            if (m_Nodes.ContainsKey(nodeId))
            {
                throw new ArgumentException(string.Format("LeafNodePackage '{0}' contains duplicated node id '{1}'", m_Name, nodeId.ToDebugString()));
            }
            m_Nodes.Add(nodeId, inNode);
        }

        #endregion // Modifications

        #region ILeafModule

        /// <summary>
        /// Attempts to retrieve the node with the specific id in this package.
        /// </summary>
        public bool TryGetNode(StringHash32 inNodeId, out TNode outNode)
        {
            return m_Nodes.TryGetValue(inNodeId, out outNode);
        }

        public override bool TryGetNode(StringHash32 inNodeId, out LeafNode outNode)
        {
            TNode node;
            bool bFound = TryGetNode(inNodeId, out node);
            outNode = node;
            return bFound;
        }

        #endregion // ILeafModule

        #region IEnumerable

        public override IEnumerator<LeafNode> GetEnumerator()
        {
            return m_Nodes.Values.GetEnumerator();
        }

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
        {
            return m_Nodes.Values.GetEnumerator();
        }

        #endregion // IEnumerable

        /// <summary>
        /// Clears this package.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            m_Nodes.Clear();
        }
    }
}