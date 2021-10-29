using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine.Scripting;

namespace ProtoAqua.Argumentation
{
    public class ArgueLink : GraphData
    {
        private Dictionary<StringHash32, StringHash32> nextNodeIds = new Dictionary<StringHash32, StringHash32>();
        private List<StringHash32> requiredVisited = new List<StringHash32>();
        private List<StringHash32> requiredUsed = new List<StringHash32>();

        #region Serialized

        // Properties
        [BlockMeta("tag"), UnityEngine.Scripting.Preserve] private StringHash32 m_Tag = null;
        [BlockMeta("showIf"), UnityEngine.Scripting.Preserve] private string m_Conditions = null;
        [BlockMeta("shortenedText"), UnityEngine.Scripting.Preserve] private string m_ShortenedText = null;

        // Text
        [BlockContent, UnityEngine.Scripting.Preserve] private string m_DisplayText = null;

        #endregion // Serialized

        #region Accessors

        public string DisplayText { get { return m_DisplayText; } }
        public string ShortenedText { get { return m_ShortenedText; } }
        public StringHash32 Tag { get { return m_Tag; } }
        public string Conditions { get { return m_Conditions; } }

        #endregion // Accessors

        public ArgueLink(string inId) : base(inId) { }

        public ArgueLink(BFBase inPlayerFact, BFDiscoveredFlags inFlags)
            : base(inPlayerFact.name)
        {
            m_DisplayText = BFType.GenerateSentence(inPlayerFact, inFlags);
        }
    }
}
