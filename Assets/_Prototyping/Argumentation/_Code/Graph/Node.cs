using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine.Scripting;


namespace ProtoAqua.Argumentation
{
    public class Node : GraphData
    {
        private readonly Dictionary<StringHash32, StringHash32> m_LinkToNodeIds = new Dictionary<StringHash32, StringHash32>();
        private NodeFlags m_Flags;

        #region Serialized

        private List<string> m_InLinkToNodeIds = new List<string>();

        // Ids
        [BlockMeta("defaultNodeId")] private StringHash32 m_DefaultNodeId = "node.default";
        [BlockMeta("invalidNodeId")] private StringHash32 m_InvalidNodeId = null;
        [BlockMeta("nextNodeId")] private StringHash32 m_NextNodeId = null;

        [BlockMeta("showClaims"), Preserve]
        private void SetClaimsActive()
        {
            m_Flags |= NodeFlags.ShowClaims;
        }
        

        // Text
        [BlockContent] private string m_DisplayText = null;

        #endregion // Serialized

        #region Accessors

        public string DisplayText
        {
            get { return m_DisplayText; }
        }

        public StringHash32 DefaultNodeId
        {
            get { return m_DefaultNodeId; }
        }

        public StringHash32 InvalidNodeId
        {
            get { return m_InvalidNodeId; }
        }

        public StringHash32 NextNodeId
        {
            get { return m_NextNodeId; }
        }

        public bool IsInvalid
        {
            get { return (m_Flags & NodeFlags.IsInvalid) != 0; }
        }

        public bool ShowClaims
        {
            get { return (m_Flags & NodeFlags.ShowClaims) != 0; }
        }

        #endregion // Accessors

        public Node(string inId) : base(inId)
        {
            if (inId.Contains("invalid"))
            {
                m_Flags |= NodeFlags.IsInvalid;
            }
        }

        public StringHash32 GetNextNodeId(StringHash32 id)
        {
            if (m_LinkToNodeIds.TryGetValue(id, out StringHash32 nextNodeId))
            {
                return nextNodeId;
            }

            return null;
        }

        // Checks if a given link id is a valid response to this node
        public bool CheckResponse(StringHash32 id)
        {
            return m_LinkToNodeIds.ContainsKey(id);
        }

        [BlockMeta("linkToNode"), Preserve]
        private void AddLinkToNode(StringHash32 inLink, StringHash32 inNode)
        {
            m_LinkToNodeIds.Add(inLink, inNode);
        }

        static private readonly char[] CommaSplit = new char[] { ',' };
    }

    [Flags]
    public enum NodeFlags : byte
    {
        IsInvalid = 0x01,
        ShowClaims = 0x02
    }
}
