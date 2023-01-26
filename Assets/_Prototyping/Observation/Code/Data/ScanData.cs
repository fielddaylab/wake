using System;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine.Scripting;
using Aqua;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using Leaf;
using System.Collections.Generic;

namespace ProtoAqua.Observation
{
    public class ScanData : IDataBlock, IValidatable
    {
        #region Serialized

        // Ids
        private StringHash32 m_Id = null;
        private TextId m_HeaderId = null;
        private TextId m_DescId = null;

        // Properties
        private ScanDataFlags m_Flags = 0;
        [BlockMeta("scanDuration"), UnityEngine.Scripting.Preserve] private float m_ScanDuration = 1;
        [BlockMeta("freezeDisplay"), UnityEngine.Scripting.Preserve] private float m_FreezeDisplay = 0;
        [BlockMeta("typingDuration"), UnityEngine.Scripting.Preserve] private float m_TypingDuration = 1;

        // Text
        [BlockMeta("header"), UnityEngine.Scripting.Preserve] private string m_HeaderText = null;
        [BlockContent, UnityEngine.Scripting.Preserve] private string m_DescText = null;

        // Links
        [BlockMeta("image"), UnityEngine.Scripting.Preserve] private string m_ImagePath = null;
        [BlockMeta("logbook"), UnityEngine.Scripting.Preserve] private StringHash32 m_LogbookId = null;
        [BlockMeta("bestiary"), UnityEngine.Scripting.Preserve] private StringHash32 m_BestiaryId = null;
        private BFTypeId m_DynamicFactType = BFTypeId._COUNT;
        private StringHash32[] m_BestiaryFactIds = null;

        // Requirements
        private VariantComparison[] m_Requirements = null;
        [BlockMeta("fallback"), UnityEngine.Scripting.Preserve] private StringHash32 m_Fallback = null;

        #endregion // Serialized

        public ScanData(string inFullId)
        {
            m_Id = inFullId;
            m_HeaderId = m_Id.Concat(".header");
            m_DescId = m_Id.Concat(".body");
        }

        public StringHash32 Id() { return m_Id; }

        public ScanDataFlags Flags() { return m_Flags; }
        public float ScanDuration() { return m_ScanDuration; }
        public float FreezeDisplay() { return m_FreezeDisplay; }
        public float TypingDuration() { return m_TypingDuration; }

        public string Header() { return Services.Loc?.Localize(m_HeaderId, m_HeaderText); }
        public string Text() { return Services.Loc?.Localize(m_DescId, m_DescText); }

        public string ImagePath() { return m_ImagePath; }
        public StringHash32 LogbookId() { return m_LogbookId; }
        public StringHash32 BestiaryId() { return m_BestiaryId; }

        public ListSlice<StringHash32> FactIds() { return m_BestiaryFactIds; }
        public BFTypeId DynamicFactType() { return m_DynamicFactType; }

        public ListSlice<VariantComparison> Requirements() { return m_Requirements; }
        public StringHash32 FallbackId() { return m_Fallback; }

        #region Scan

        [BlockMeta("tool"), UnityEngine.Scripting.Preserve]
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

        [BlockMeta("factsOfType"), Preserve]
        private void SetDynamicFacts(BFTypeId inTypeId)
        {
            m_Flags |= ScanDataFlags.DynamicFactType;
            m_DynamicFactType = inTypeId;
        }

        [BlockMeta("requires"), UnityEngine.Scripting.Preserve]
        private void SetRequirements(StringSlice inRequirements)
        {
            m_Requirements = LeafUtils.ParseConditionsList(inRequirements);
        }

        [BlockMeta("noDisplay"), UnityEngine.Scripting.Preserve]
        private void SetNoDisplay()
        {
            m_Flags |= ScanDataFlags.DoNotShow;
        }

        void IValidatable.Validate()
        {
            #if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying) {
                return;
            }
            #endif // UNITY_EDITOR

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
        
        #if UNITY_EDITOR

        internal KeyValuePair<StringHash32, string> ExportHeader()
        {
            return new KeyValuePair<StringHash32, string>(m_HeaderId, m_HeaderText);
        }

        internal KeyValuePair<StringHash32, string> ExportText()
        {
            return new KeyValuePair<StringHash32, string>(m_DescId, m_DescText);
        }

        #endif // UNITY_EDITOR

        #region Default

        static public readonly ScanData Error;
        static public readonly ScanData Fake;

        static ScanData()
        {
            Error = new ScanData("");
            Error.m_HeaderText = "MISSING SCAN DATA";
            Error.m_DescText = "Scan is missing";

            Fake = new ScanData("fake");
            Fake.m_HeaderText = "";
            Fake.m_DescText = "";
            Fake.m_Flags |= ScanDataFlags.DoNotShow | ScanDataFlags.Important;
            Fake.m_ScanDuration = 3;
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

        DynamicFactType = 0x40,
        DoNotShow = 0x80,
    }
}