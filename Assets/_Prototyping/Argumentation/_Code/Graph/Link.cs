using System.Collections.Generic;
using BeauUtil.Blocks;
using UnityEngine.Scripting;


namespace ProtoAqua.Argumentation
{
    public class Link : GraphData
    {
        public static int count = 0;
        public int Index { get; set; }

        private Dictionary<string, string> nextNodeIds = new Dictionary<string, string>();
        private List<string> requiredVisited = new List<string>();
        private List<string> requiredUsed = new List<string>();

        #region Serialized

        private List<string> m_InNextNodeIds = new List<string>();

        // Properties
        [BlockMeta("tag")] private string m_Tag = null;
        [BlockMeta("conditions")] private string m_Conditions = null;
        [BlockMeta("type")] private string m_Type = null;
        [BlockMeta("shortenedText")] private string m_ShortenedText = null;

        // Ids
        [BlockMeta("invalidNodeId")] private string m_InvalidNodeId = null;
        [BlockMeta("conditionsNotMetId")] private string m_ConditionsNotMetId = null;
        [BlockMeta("nextNodeId"), Preserve]
        private void AddNextNodeIds(string line)
        {
            m_InNextNodeIds.Add(line);
        }

        // Text
        [BlockContent] private string m_DisplayText = null;

        #endregion // Serialized

        #region Accessors

        public string DisplayText
        {
            get { return m_DisplayText; }
        }

        public string ShortenedText
        {
            get { return m_ShortenedText; }
        }

        public string Tag
        {
            get { return m_Tag; }
        }
        public string Type
        {
            get { return m_Type; }
        }


        public string InvalidNodeId
        {
            get { return m_InvalidNodeId; }
        }

        public string ConditionsNotMetId
        {
            get { return m_ConditionsNotMetId; }
        }

        public Dictionary<string, string> NextNodeIds
        {
            get { return nextNodeIds; }
        }

        public List<string> RequiredVisited
        {
            get { return requiredVisited; }
        }

        public List<string> RequiredUsed
        {
            get { return requiredUsed; }
        }

        #endregion // Accessors

        public Link(string inId) : base(inId) { }

        public void InitializeLink()
        {
            
            ParseNextNodeIds(m_InNextNodeIds);

            if (m_Conditions != null)
            {
                ParseConditions(m_Conditions);
            }

            Index = ++count;
           
        }

        // Given a node id, return the respsective node id that this link connects it to
        public string GetNextNodeId(string id)
        {
            if (nextNodeIds.TryGetValue(id, out string nextNodeId))
            {
                return nextNodeId;
            }

            return null;
        }

        private void ParseNextNodeIds(List<string> inNextNodeIds)
        {
            nextNodeIds = new Dictionary<string, string>();
            foreach (string ids in inNextNodeIds)
            {
                string[] parsedIds = ids.Split(',');
                nextNodeIds.Add(parsedIds[0], parsedIds[1].Trim());
            }
        }

        private void ParseConditions(string inConditions)
        {
             
            string[] conditions = inConditions.Split(',');

            foreach (string condition in conditions)
            {
                if (condition.StartsWith("node"))
                {
                    requiredVisited.Add(condition);
                }
                else if (condition.StartsWith("link"))
                {
                    requiredUsed.Add(condition);
                }
            }
        } 
    }
}
