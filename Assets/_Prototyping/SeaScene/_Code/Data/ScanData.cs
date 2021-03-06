using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine.Scripting;
using BeauUtil.Variants;
using BeauPools;
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
        private VariantModification[] m_OnScanModifications;

        // Text
        [BlockMeta("header")] private string m_HeaderText = null;
        [BlockContent] private string m_DescText = null;

        // Links
        [BlockMeta("spriteId")] private string m_SpriteId = null;

        [BlockMeta("logbook")] private StringHash32 m_LogbookId = null;
        [BlockMeta("bestiary")] private StringHash32 m_BestiaryId = null;

        #endregion // Serialized

        public ScanData(string inFullId)
        {
            m_Id = inFullId;
        }

        public StringHash32 Id() { return m_Id; }

        public ScanDataFlags Flags() { return m_Flags; }
        public int ScanSpeed() { return m_ScanDuration; }
        public VariantModification[] OnScanModifications() { return m_OnScanModifications; }

        public string Header() { return m_HeaderText; }
        public string Text() { return m_DescText; }

        public string SpriteId() { return m_SpriteId; }
        public StringHash32 LogbookId() { return m_LogbookId; }
        public StringHash32 BestiaryId() { return m_BestiaryId; }

        #region Scan

        [BlockMeta("important"), Preserve]
        private void SetImportant(bool inbImportant = true)
        {
            if (inbImportant)
                m_Flags |= ScanDataFlags.Important;
            else
                m_Flags &= ~ScanDataFlags.Important;
        }

        [BlockMeta("setOnScan"), Preserve]
        private void OnScan(StringSlice inData)
        {
            TempList16<StringSlice> split = new TempList16<StringSlice>();
            int slices = inData.Split(Parsing.CommaChar, StringSplitOptions.RemoveEmptyEntries, ref split);
            m_OnScanModifications = ArrayUtils.MapFrom(split, (s) => {
                VariantModification modification;
                if (!VariantModification.TryParse(s, out modification))
                {
                    Debug.LogErrorFormat("[ScanData] Unable to parse variable modification from '{0}'", s);
                }
                return modification;
            });
        }

        void IValidatable.Validate()
        {
            Assert.True(m_BestiaryId.IsEmpty || Services.Assets.Bestiary.HasId(m_BestiaryId),
                "Scan '{0}' was linked to invalid bestiary entry '{1}'", m_Id.ToDebugString(), m_BestiaryId.ToDebugString());
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