using System.Collections.Generic;

namespace ProtoAqua.Argumentation
{
    public class Link
    {
        public static int count = 0;

        public int Index { get; set; }

        private string id;
        private string displayText;
        private string tag;
        private string conditionsNotMetId;
        private Dictionary<string, string> nextNodeIds = new Dictionary<string, string>();
        private List<string> requiredVisited = new List<string>();
        private List<string> requiredUsed = new List<string>();
        private bool used;

        public Link(string inId, string inDisplayText, string inTag, string inConditionsNotMetId, 
            List<string> inNextNodeIds, string inConditions)
        {
            id = inId;
            displayText = inDisplayText;
            tag = inTag;
            conditionsNotMetId = inConditionsNotMetId;
            
            ParseNextNodeIds(inNextNodeIds);

            if (inConditions != null)
            {
                ParseConditions(inConditions);
            }
            
            Index = ++count;
        }
        
        #region Accessors

        public string Id
        {
            get { return id; }
        }

        public string DisplayText
        {
            get { return displayText; }
        }

        public string Tag
        {
            get { return tag; }
        }

        public string ConditionsNotMetId
        {
            get { return conditionsNotMetId; }
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
        
        public bool Used
        {
            get { return used; }
            set { used = value; }
        }

        #endregion // Accessors

        private void ParseNextNodeIds(List<string> inNextNodeIds)
        {
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

        // Given a node id, return the respsective node id that this link connects it to
        public string GetNextNodeId(string id)
        {
            if (nextNodeIds.TryGetValue(id, out string nextNodeId))
            {
                return nextNodeId;
            }

            return null;
        }
    }
}
