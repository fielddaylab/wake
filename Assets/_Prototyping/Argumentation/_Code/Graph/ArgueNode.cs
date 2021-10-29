using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using UnityEngine.Scripting;


namespace ProtoAqua.Argumentation
{
    public class ArgueNode : GraphData
    {
        public struct PotentialLink {
            public StringHash32 LinkId;
            public StringHash32 NodeId;
            public string Conditions;
        }

        public struct PotentialNext {
            public StringHash32 NodeId;
            public string Conditions;
        }

        private readonly RingBuffer<PotentialLink> m_LinkEntries = new RingBuffer<PotentialLink>();
        private readonly RingBuffer<PotentialNext> m_NextEntries = new RingBuffer<PotentialNext>();
        private readonly RingBuffer<StringHash32> m_SetFlags = new RingBuffer<StringHash32>();
        private readonly RingBuffer<StringHash32> m_UnsetFlags = new RingBuffer<StringHash32>();
        private ArgueNodeFlags m_Flags;

        #region Serialized

        // Ids
        [BlockMeta("invalidNodeId"), UnityEngine.Scripting.Preserve] private StringHash32 m_InvalidNodeId = null;

        [BlockMeta("showClaims"), Preserve]
        private void SetClaimsActive() {
            m_Flags |= ArgueNodeFlags.ShowClaims;
        }

        [BlockMeta("cancel"), Preserve]
        private void SetCancel() {
            m_Flags |= ArgueNodeFlags.CancelArgue;
        }

        // Text
        private RingBuffer<string> m_DisplayTexts = new RingBuffer<string>();

        #endregion // Serialized

        #region Accessors

        public ListSlice<string> DisplayTexts {
            get { return m_DisplayTexts; }
        }

        public StringHash32 InvalidNodeId {
            get { return m_InvalidNodeId; }
        }

        public bool IsInvalid {
            get { return (m_Flags & ArgueNodeFlags.IsInvalid) != 0; }
        }

        public bool ShowClaims {
            get { return (m_Flags & ArgueNodeFlags.ShowClaims) != 0; }
        }

        public bool CancelFlow {
            get { return (m_Flags & ArgueNodeFlags.CancelArgue) != 0; }
        }

        public ListSlice<PotentialLink> PotentialLinks {
            get { return m_LinkEntries; }
        }

        public ListSlice<PotentialNext> PotentialNexts {
            get { return m_NextEntries; }
        }

        public ListSlice<StringHash32> FlagsToSet {
            get { return m_SetFlags; }
        }

        public ListSlice<StringHash32> FlagsToUnset {
            get { return m_UnsetFlags; }
        }

        #endregion // Accessors

        public ArgueNode(string inId) : base(inId) {
            if (inId.Contains("invalid")) {
                m_Flags |= ArgueNodeFlags.IsInvalid;
            }
        }

        [BlockContent(BlockContentMode.LineByLine)]
        private void AddTextLine(StringSlice inLine) {
            if (!inLine.IsEmpty) {
                m_DisplayTexts.PushBack(inLine.ToString());
            }
        }

        [BlockMeta("linkToNode"), Preserve]
        private void AddLinkToNode(StringSlice inLinkData) {
            int commaIdx = inLinkData.IndexOf(',');
            StringSlice linkId = inLinkData.Substring(0, commaIdx).Trim();
            StringSlice secondHalf = inLinkData.Substring(commaIdx + 1);

            StringSlice nodeId;
            StringSlice conditions;
            int semiIdx = secondHalf.IndexOf(';');
            if (semiIdx >= 0) {
                nodeId = secondHalf.Substring(0, semiIdx).Trim();
                conditions = secondHalf.Substring(semiIdx + 1).Trim();
            } else {
                nodeId = secondHalf.Trim();
                conditions = null;
            }

            Assert.False(linkId.IsEmpty, "linkToNode command on node '{0}' has no link id", Id);
            Assert.False(nodeId.IsEmpty, "linkToNode command on node '{0}' with link '{1}' has no node id", Id, linkId);

            PotentialLink link;
            link.LinkId = linkId;
            link.NodeId = nodeId;
            link.Conditions = conditions.ToString();

            m_LinkEntries.PushBack(link);
        }

        [BlockMeta("nextNodeId"), UnityEngine.Scripting.Preserve]
        private void AddNextTonode(StringSlice inNextData) {
            StringSlice nodeId;
            StringSlice conditions;
            int semiIdx = inNextData.IndexOf(';');
            if (semiIdx >= 0) {
                nodeId = inNextData.Substring(0, semiIdx).Trim();
                conditions = inNextData.Substring(semiIdx + 1).Trim();
            } else {
                nodeId = inNextData.Trim();
                conditions = null;
            }

            Assert.False(nodeId.IsEmpty, "nextNodeId command on node '{0}' has no node id", Id);

            PotentialNext next;
            next.NodeId = nodeId;
            next.Conditions = conditions.ToString();
            m_NextEntries.PushBack(next);
        }

        [BlockMeta("setFlag"), UnityEngine.Scripting.Preserve]
        private void SetFlag(StringHash32 inId) {
            m_SetFlags.PushBack(inId);
        }

        [BlockMeta("unsetFlag"), UnityEngine.Scripting.Preserve]
        private void UnsetFlag(StringHash32 inId) {
            m_UnsetFlags.PushBack(inId);
        }

        static private readonly char[] CommaSplit = new char[] { ',' };
    }

    [Flags]
    public enum ArgueNodeFlags : byte
    {
        IsInvalid = 0x01,
        ShowClaims = 0x02,
        CancelArgue = 0x04
    }
}
