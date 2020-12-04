using System;
using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;

namespace Aqua
{
    public class LocNode : IDataBlock
    {
        private readonly StringHash32 m_Id;
        [BlockContent] private string m_Content = string.Empty;

        public LocNode(StringHash32 inId)
        {
            m_Id = inId;
        }

        public StringHash32 Id() { return m_Id; }
        public string Content() { return m_Content; }

        static public implicit operator string(LocNode inNode)
        {
            return inNode?.m_Content;
        }
    }
}