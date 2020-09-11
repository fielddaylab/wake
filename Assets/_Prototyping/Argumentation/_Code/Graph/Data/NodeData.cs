using BeauUtil.Blocks;

namespace ProtoAqua.Argumentation
{
    public class NodeData : GraphData
    {
        #region Serialized

        [BlockMeta("rootNode")] private bool m_RootNode = false;

        // Ids
        [BlockMeta("responseIds")] private string m_ResponseIds = null;

        // Text
        [BlockContent] private string m_DisplayText = null;

        #endregion // Serialized

        public NodeData(string inId) : base(inId) { }

        #region Accessors

        public bool RootNode() { return m_RootNode; }

        public string ResponseIds() { return m_ResponseIds; }

        public string DisplayText() { return m_DisplayText; }

        #endregion // Accessors
    }
}
