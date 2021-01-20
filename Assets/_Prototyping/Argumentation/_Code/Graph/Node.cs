using System.Collections.Generic;
using BeauUtil.Blocks;
using UnityEngine.Scripting;


namespace ProtoAqua.Argumentation
{
    public class Node : GraphData
    {
        private List<string> responses = new List<string>();
        private Dictionary<string, string> linkToNodeIds = new Dictionary<string, string>();

        #region Serialized

        private List<string> m_InLinkToNodeIds = new List<string>();

        // Ids
        [BlockMeta("defaultNodeId")] private string m_DefaultNodeId = "node.default";
        [BlockMeta("responseIds")] private string m_ResponseIds = null;
        [BlockMeta("invalidNodeId")] private string m_InvalidNodeId = null;
        [BlockMeta("nextNodeId")] private string m_NextNodeId = null;
        [BlockMeta("linkToNode"), Preserve]
        private void AddNextNodeIds(string line)
        {
            m_InLinkToNodeIds.Add(line);
        }

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

        public string InvalidNodeId
        {
            get { return m_InvalidNodeId; }
        }
        public string NextNodeId
        {
            get { return m_NextNodeId; }
        }

        #endregion // Accessors

        public Node(string inId) : base(inId) { }

        public void InitializeNode()
        {
            ParseLinkToNodeIds(m_InLinkToNodeIds);
            if (m_ResponseIds != null)
            {
                ParseResponses(m_ResponseIds);
            }
        }

        public string GetNextNodeId(string id) {
            if (linkToNodeIds.TryGetValue(id, out string nextNodeId))
            {
                return nextNodeId;
            }

            return null;
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

        private void ParseLinkToNodeIds(List<string> inLinkToNodeIds) {
            linkToNodeIds = new Dictionary<string, string>();
            foreach (string ids in inLinkToNodeIds)
            {
                string[] parsedIds = ids.Split(',');
                linkToNodeIds.Add(parsedIds[0], parsedIds[1].Trim());
            }
            

        }
    }
}
