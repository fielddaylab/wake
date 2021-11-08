using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using Aqua;
using Aqua.Profile;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;

namespace Aqua.Scripting
{
    internal class FunctionSet : IReadOnlyCollection<ScriptNode>
    {
        private readonly List<ScriptNode> m_FunctionNodes = new List<ScriptNode>(16);

        #region Add/Remove

        /// <summary>
        /// Adds a node to the response set.
        /// </summary>
        public bool AddNode(ScriptNode inNode)
        {
            if (m_FunctionNodes.Contains(inNode))
            {
                Log.Error("[FunctionSet] Cannot add node '{0} twice", inNode.Id());
                return false;
            }

            int prevCount = m_FunctionNodes.Count;

            m_FunctionNodes.Add(inNode);
            return true;
        }

        /// <summary>
        /// Removes a node from the response set.
        /// </summary>
        public bool RemoveNode(ScriptNode inNode)
        {
            return m_FunctionNodes.FastRemove(inNode);
        }

        #endregion // Add/Remove

        #region Locating

        /// <summary>
        /// Returns the nodes for this set.
        /// </summary>
        public int GetNodes(StringHash32 inTarget, ICollection<ScriptNode> outNodes)
        {
            ScriptNode node;
            int count = 0;
            for(int nodeIdx = 0, nodeCount = m_FunctionNodes.Count; nodeIdx < nodeCount; ++nodeIdx)
            {
                node = m_FunctionNodes[nodeIdx];

                if (!node.Package().IsActive())
                    continue;
                
                // not the right target
                if (!inTarget.IsEmpty && inTarget != node.TargetId())
                    continue;

                outNodes.Add(node);
                ++count;
            }

            return count;
        }

        #endregion // Locating

        #region ICollection

        public int Count { get { return m_FunctionNodes.Count; } }

        public IEnumerator<ScriptNode> GetEnumerator()
        {
            return m_FunctionNodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // ICollection
    }
}