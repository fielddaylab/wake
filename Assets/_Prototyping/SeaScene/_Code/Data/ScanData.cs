using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using BeauUtil.Blocks;

namespace ProtoAqua.Observation
{
    public class ScanData : IDataBlock
    {
        static public readonly string NullId = "[null]";

        #region Serialized

        // Ids
        private string m_SelfId = null;
        private string m_FullId = null;

        // Properties
        private ScanDataFlags m_Flags = 0;
        [BlockMeta("scanDuration")] private int m_ScanDuration = 1;

        // Text
        [BlockMeta("header")] private string m_HeaderText = null;
        [BlockContent] private string m_DescText = null;

        // Links
        [BlockMeta("spriteId")] private string m_SpriteId = null;
        [BlockMeta("logbook")] private string m_LogbookId = null;
        [BlockMeta("eventEntrypoint")] private string m_EventEntrypoint = null;

        #endregion // Serialized

        public ScanData(string inSelfId, string inFullId)
        {
            m_SelfId = inSelfId;
            m_FullId = inFullId;
        }

        public string Id() { return m_FullId; }
        public string SelfId() { return m_SelfId; }

        public ScanDataFlags Flags() { return m_Flags; }
        public int ScanSpeed() { return m_ScanDuration; }

        public string Header() { return m_HeaderText; }
        public string Text() { return m_DescText; }

        public string SpriteId() { return m_SpriteId; }
        public string LogbookId() { return m_LogbookId; }

        public string EventEntrypoint() { return m_EventEntrypoint; }

        #region Scan

        [BlockMeta("important")]
        private void SetImportant(bool inbImportant = true)
        {
            if (inbImportant)
                m_Flags |= ScanDataFlags.Important;
            else
                m_Flags &= ~ScanDataFlags.Important;
        }

        #endregion // Scan
    }

    [Flags]
    public enum ScanDataFlags : byte
    {
        Actor       = 0x01,
        Environment = 0x02,
        Character   = 0x04,

        Important   = 0x10,
    }
}