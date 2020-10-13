using System.Collections.Generic;
using BeauUtil.Blocks;

namespace ProtoAqua.Argumentation
{
    public class Node : GraphData
    {
        private List<string> responses = new List<string>();

        #region Serialized

        // Ids
        [BlockMeta("defaultNodeId")] private string m_DefaultNodeId = "node.default";
        [BlockMeta("responseIds")] private string m_ResponseIds = null;
        [BlockMeta("invalidNodeId")] private string m_InvalidNodeId = "node.default";

        // Text
        [BlockContent] private string m_DisplayText = null;

        #endregion // Serialized

        #region Accessors

        public string DisplayText
        {
            get { return m_DisplayText; }
        }

        public string DefaultNodeId
        {
            get { return m_DefaultNodeId; }
        }

        public List<string> Responses
        {
            get { return responses; }
        }

        #endregion // Accessors

        public Node(string inId) : base(inId) { }

        public void InitializeNode()
        {
            if (m_ResponseIds != null)
            {
                ParseResponses(m_ResponseIds);
            }
        }

        // Checks if a given link id is a valid response to this node
        public bool CheckResponse(string id)
        {
            foreach (string response in responses)
            {
                if (response.Equals(id))
                {
                    return true;
                }
            }

            return false;
        }

        private void ParseResponses(string inResponses)
        {
            string[] splitResponses = inResponses.Split(',');

            foreach (string response in splitResponses)
            {
                responses.Add(response.Trim());
            }
        }
    }
}
