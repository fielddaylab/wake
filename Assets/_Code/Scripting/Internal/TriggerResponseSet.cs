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

namespace Aqua.Scripting
{
    internal class TriggerResponseSet : IReadOnlyCollection<ScriptNode>
    {
        private readonly List<ScriptNode> m_TriggerNodes = new List<ScriptNode>(16);
        private bool m_Sorted = true;

        #region Add/Remove

        /// <summary>
        /// Adds a node to the response set.
        /// </summary>
        public bool AddNode(ScriptNode inNode)
        {
            if (m_TriggerNodes.Contains(inNode))
            {
                Debug.LogErrorFormat("[TriggerResponseSet] Cannot add node '{0} twice", inNode.Id().ToDebugString());
                return false;
            }

            int prevCount = m_TriggerNodes.Count;

            m_TriggerNodes.Add(inNode);
            m_Sorted &= prevCount == 0 || m_TriggerNodes[prevCount - 1].TriggerData.Score < inNode.TriggerData.Score;
            return true;
        }

        /// <summary>
        /// Removes a node from the response set.
        /// </summary>
        public bool RemoveNode(ScriptNode inNode)
        {
            if (m_TriggerNodes.FastRemove(inNode))
            {
                m_Sorted = false;
                return true;
            }

            return false;
        }

        #endregion // Add/Remove

        #region Locating

        /// <summary>
        /// Returns the highest-scoring nodes for this response set.
        /// </summary>
        public int GetHighestScoringNodes(IVariantResolver inResolver, object inContext, ScriptingData inScriptData, StringHash32 inTarget, Dictionary<StringHash32, ScriptThread> inTargetStates, ICollection<ScriptNode> outNodes, ref int ioMinScore)
        {
            Optimize();

            ScriptNode node;
            TriggerNodeData triggerData;
            int count = 0;
            for(int nodeIdx = 0, nodeCount = m_TriggerNodes.Count; nodeIdx < nodeCount; ++nodeIdx)
            {
                node = m_TriggerNodes[nodeIdx];
                triggerData = node.TriggerData;

                // score cutoff
                if (triggerData.Score < ioMinScore)
                    break;

                // not the right target
                if (!inTarget.IsEmpty && inTarget != node.TargetId())
                    continue;

                // cannot play during cutscene
                if ((node.Flags() & ScriptNodeFlags.SuppressDuringCutscene) != 0 && Services.UI.IsLetterboxed())
                    continue;

                // cannot play due to once
                if (triggerData.OnceLevel != PersistenceLevel.Untracked && inScriptData.HasSeen(node.Id(), triggerData.OnceLevel))
                    continue;

                // cannot play due to repetition
                if (triggerData.RepeatDuration > 0 && inScriptData.HasRecentlySeen(node.Id(), triggerData.RepeatDuration))
                    continue;

                // cannot play due to priority
                if (node.TargetId() != StringHash32.Null)
                {
                    ScriptThread currentThread;
                    if (inTargetStates.TryGetValue(node.TargetId(), out currentThread) && currentThread.Priority() > triggerData.TriggerPriority)
                        continue;
                }

                // cannot play due to conditions
                if (triggerData.Conditions != null)
                {
                    bool bFailed = false;
                    for(int condIdx = 0, condCount = triggerData.Conditions.Length; condIdx < condCount; ++condIdx)
                    {
                        if (!triggerData.Conditions[condIdx].Evaluate(inResolver, inContext))
                        {
                            bFailed = true;
                            break;
                        }
                    }

                    if (bFailed)
                        continue;
                }

                outNodes.Add(node);
                ioMinScore = triggerData.Score;
                ++count;
            }

            return count;
        }

        #endregion // Locating

        #region Sorting

        /// <summary>
        /// Optimizes the response set.
        /// </summary>
        public void Optimize()
        {
            if (m_Sorted)
                return;

            m_TriggerNodes.Sort(ScriptNodeComparer);
            m_Sorted = true;
        }

        static private readonly Comparison<ScriptNode> ScriptNodeComparer = (l, r) => {
            return Math.Sign(r.TriggerData.Score - l.TriggerData.Score);
        };

        #endregion // Sorting

        #region ICollection

        public int Count { get { return m_TriggerNodes.Count; } }

        public IEnumerator<ScriptNode> GetEnumerator()
        {
            return m_TriggerNodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // ICollection
    }
}