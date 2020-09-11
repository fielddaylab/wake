using System.Collections.Generic;
using BeauUtil.Blocks;

namespace ProtoAqua.Argumentation
{
    public class LinkData : GraphData
    {
        #region Serialized

        private List<string> m_NextNodeIds = new List<string>();

        // Properties
        [BlockMeta("tag")] private string m_Tag = null;
        [BlockMeta("conditions")] private string m_Conditions = null;

        // Ids
        [BlockMeta("conditionsNotMetId")] private string m_ConditionsNotMetId = null;
        
        [BlockMeta("nextNodeId")]
        private void AddNextNodeIds(string line)
        {
            m_NextNodeIds.Add(line);
        }

        // Text
        [BlockContent] private string m_DisplayText = null;

        #endregion // Serialized

        public LinkData(string inId) : base(inId) { }

        #region Accessors

        public string Tag() { return m_Tag; }
        public string Conditions() { return m_Conditions; }

        public string ConditionsNotMetId() { return m_ConditionsNotMetId; }
        public List<string> NextNodeIds() { return m_NextNodeIds; }

        public string DisplayText() { return m_DisplayText; }

        #endregion // Accessors
    }
}
