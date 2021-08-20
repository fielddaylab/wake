using System;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine.Scripting;
using Aqua;
using BeauUtil.Debugger;

namespace ProtoAqua.Observation
{
    public class ScanData : IDataBlock, IValidatable
    {
        #region Serialized

        // Ids
        private StringHash32 m_Id = null;

        // Properties
        private ScanDataFlags m_Flags = 0;
        [BlockMeta("scanDuration")] private int m_ScanDuration = 1;

        // Text
        [BlockMeta("header")] private string m_HeaderText = null;
        [BlockContent] private string m_DescText = null;

        // Links
        [BlockMeta("spriteId")] private StringHash32 m_SpriteId = null;
        [BlockMeta("logbook")] private StringHash32 m_LogbookId = null;
        [BlockMeta("bestiary")] private StringHash32 m_BestiaryId = null;
        private StringHash32[] m_BestiaryFactIds = null;

        // Requirements
        [BlockMeta("requires")] private string m_Requirements = null;
        [BlockMeta("fallback")] private StringHash32 m_Fallback = null;

        #endregion // Serialized

        public ScanData(string inFullId)
        {
            m_Id = inFullId;
        }

        public StringHash32 Id() { return m_Id; }

        public ScanDataFlags Flags() { return m_Flags; }
        public int ScanSpeed() { return m_ScanDuration; }

        public string Header() { return m_HeaderText; }
        public string Text() { return m_DescText; }

        public StringHash32 SpriteId() { return m_SpriteId; }
        public StringHash32 LogbookId() { return m_LogbookId; }
        public StringHash32 BestiaryId() { return m_BestiaryId; }

        public ListSlice<StringHash32> FactIds() { return m_BestiaryFactIds; }

        public StringSlice Requirements() { return m_Requirements; }
        public StringHash32 FallbackId() { return m_Fallback; }

        #region Scan

        [BlockMeta("tool")]
        private void ActivateTool(bool inbActivate)
        {
            m_Flags |= ScanDataFlags.Tool;
            if (inbActivate)
                m_Flags |= ScanDataFlags.ActivateTool;
        }

        [BlockMeta("important"), Preserve]
        private void SetImportant(bool inbImportant = true)
        {
            if (inbImportant)
                m_Flags |= ScanDataFlags.Important;
            else
                m_Flags &= ~ScanDataFlags.Important;
        }

        [BlockMeta("facts"), Preserve]
        private void SetFacts(StringSlice inData)
        {
            TempList16<StringSlice> split = new TempList16<StringSlice>();
            int slices = inData.Split(Parsing.CommaChar, StringSplitOptions.RemoveEmptyEntries, ref split);
            m_BestiaryFactIds = ArrayUtils.MapFrom(split, (s) => {
                return new StringHash32(s.Trim());
            });
        }

        void IValidatable.Validate()
        {
            Assert.True(m_BestiaryId.IsEmpty || Services.Assets.Bestiary.HasId(m_BestiaryId),
                "Scan '{0}' was linked to unknown bestiary entry '{1}'", m_Id, m_BestiaryId);

            if (m_BestiaryFactIds != null)
            {
                foreach(var factId in m_BestiaryFactIds)
                {
                    Assert.True(Services.Assets.Bestiary.HasFactWithId(factId),
                        "Scan '{0}' was linked to unknown bestiary fact '{1}'", m_Id, factId);
                }
            }
        }

        #endregion // Scan

        #region Default

        static public readonly ScanData Error;

        static ScanData()
        {
            Error = new ScanData("");
            Error.m_HeaderText = "MISSING SCAN DATA";
            Error.m_DescText = "Scan is missing";
        }

        #endregion // Default
    }

    [Flags]
    public enum ScanDataFlags : byte
    {
        Actor       = 0x01,
        Environment = 0x02,
        Character   = 0x04,
        Tool        = 0x08,

        Important   = 0x10,
        ActivateTool = 0x20,
    }
}