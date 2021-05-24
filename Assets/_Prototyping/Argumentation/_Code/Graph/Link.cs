using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine.Scripting;


namespace ProtoAqua.Argumentation
{
    public class Link : GraphData
    {
        private Dictionary<StringHash32, StringHash32> nextNodeIds = new Dictionary<StringHash32, StringHash32>();
        private List<StringHash32> requiredVisited = new List<StringHash32>();
        private List<StringHash32> requiredUsed = new List<StringHash32>();

        #region Serialized

        // Properties
        [BlockMeta("tag")] private string m_Tag = null;
        [BlockMeta("conditions")] private string m_Conditions = null;
        [BlockMeta("type")] private string m_Type = null;
        [BlockMeta("shortenedText")] private string m_ShortenedText = null;

        // Ids
        [BlockMeta("invalidNodeId")] private StringHash32 m_InvalidNodeId = null;
        [BlockMeta("conditionsNotMetId")] private StringHash32 m_ConditionsNotMetId = null;

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

        public StringHash32 InvalidNodeId
        {
            get { return m_InvalidNodeId; }
        }

        public StringHash32 ConditionsNotMetId
        {
            get { return m_ConditionsNotMetId; }
        }

        public List<StringHash32> RequiredVisited
        {
            get { return requiredVisited; }
        }

        public List<StringHash32> RequiredUsed
        {
            get { return requiredUsed; }
        }

        #endregion // Accessors

        public Link(string inId) : base(inId) { }

        public Link(BFBase inPlayerFact)
            : base(inPlayerFact.name)
        {
            m_DisplayText = inPlayerFact.GenerateSentence();
        }

        public void InitializeLink()
        {
            if (m_Conditions != null)
            {
                ParseConditions(m_Conditions);
            }
        }

        // Given a node id, return the respsective node id that this link connects it to
        public StringHash32 GetNextNodeId(StringHash32 id)
        {
            if (nextNodeIds.TryGetValue(id, out StringHash32 nextNodeId))
            {
                return nextNodeId;
            }

            return null;
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
                else
                {
                    requiredUsed.Add(condition);
                }
            }
        } 
    }
}
