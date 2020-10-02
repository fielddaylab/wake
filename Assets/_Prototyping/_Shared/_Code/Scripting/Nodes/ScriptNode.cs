using System;
using BeauUtil.Blocks;
using System.Collections.Generic;
using UnityEngine.Scripting;
using BeauUtil;
using BeauPools;
using BeauUtil.Variants;
using UnityEngine;

namespace ProtoAqua.Scripting
{
    public class ScriptNode : IDataBlock
    {
        static public readonly string NullId = "[null]";

        #region Serialized

        // Ids
        private StringHash32 m_Id = null;

        // Properties
        private ScriptNodeFlags m_Flags = 0;
        private ScriptNodePackage m_Package = null;
        private TriggerNodeData m_TriggerData = null;
        private HashSet<StringHash32> m_Tags = new HashSet<StringHash32>();

        // Text
        private List<string> m_Lines = new List<string>();

        #endregion // Serialized

        public ScriptNode(ScriptNodePackage inPackage, StringHash32 inFullId)
        {
            m_Package = inPackage;
            m_Id = inFullId;
        }

        public StringHash32 Id() { return m_Id; }
        public ScriptNodeFlags Flags() { return m_Flags; }
        public ScriptNodePackage Package() { return m_Package; }

        public TriggerNodeData TriggerData { get { return m_TriggerData; } }
        public IReadOnlyList<string> Lines() { return m_Lines; }
        public IReadOnlyCollection<StringHash32> Tags() { return m_Tags; }

        public PersistenceLevel TrackingLevel()
        {
            if (m_TriggerData != null)
            {
                if (m_TriggerData.OnceLevel != PersistenceLevel.Untracked)
                    return m_TriggerData.OnceLevel;
                if (m_TriggerData.RepeatDuration > 0)
                    return PersistenceLevel.Session;
            }

            return PersistenceLevel.Untracked;
        }

        #region Parser

        [BlockContent(BlockContentMode.LineByLine), Preserve]
        private void AddContent(string inLine)
        {
            if (!string.IsNullOrEmpty(inLine))
                m_Lines.Add(inLine);
        }

        [BlockMeta("cutscene"), Preserve]
        private void SetCutscene(bool inbCutscene = true)
        {
            if (inbCutscene)
                m_Flags |= ScriptNodeFlags.Cutscene;
            else
                m_Flags &= ~ScriptNodeFlags.Cutscene;
        }

        [BlockMeta("important"), Preserve]
        private void SetImportant()
        {
            m_Flags |= ScriptNodeFlags.Important;
        }

        [BlockMeta("entrypoint"), Preserve]
        private void SetEntrypoint()
        {
            m_Flags |= ScriptNodeFlags.Entrypoint;
        }

        [BlockMeta("trigger"), Preserve]
        private void SetTriggerResponse(StringHash32 inTriggerId)
        {
            m_Flags |= ScriptNodeFlags.TriggerResponse;
            if (m_TriggerData == null)
            {
                m_TriggerData = new TriggerNodeData();
            }
            m_TriggerData.TriggerId = inTriggerId;
        }

        [BlockMeta("who"), Preserve]
        private void SetTriggerTarget(StringHash32 inTargetId)
        {
            if (m_TriggerData != null)
            {
                m_TriggerData.TargetId = inTargetId;
            }
        }

        [BlockMeta("when"), Preserve]
        private void SetTriggerConditions(StringSlice inConditionsList)
        {
            if (m_TriggerData != null)
            {
                using(PooledList<StringSlice> conditions = PooledList<StringSlice>.Create())
                {
                    int conditionsCount = inConditionsList.Split(Parsing.QuoteAwareArgSplitter, StringSplitOptions.RemoveEmptyEntries, conditions);
                    if (conditionsCount > 0)
                    {
                        m_TriggerData.Conditions = new VariantComparison[conditionsCount];
                        for(int i = 0; i < conditionsCount; ++i)
                        {
                            if (!VariantComparison.TryParse(conditions[i], out m_TriggerData.Conditions[i]))
                            {
                                Debug.LogErrorFormat("[ScriptNode] Unable to parse condition '{0}'", conditions[i]);
                            }
                        }

                        m_TriggerData.Score += conditionsCount;
                    }
                }
            }
        }

        [BlockMeta("boostScore"), Preserve]
        private void AdjustTriggerScore(int inScore)
        {
            if (m_TriggerData != null)
            {
                m_TriggerData.Score += inScore;
            }
        }

        [BlockMeta("once"), Preserve]
        private void SetOnce(StringSlice inCategory)
        {
            if (m_TriggerData != null)
            {
                m_TriggerData.RepeatDuration = 0;
                m_TriggerData.OnceLevel = inCategory.Equals("session") ? PersistenceLevel.Session : PersistenceLevel.Profile;
            }
        }

        [BlockMeta("repeat"), Preserve]
        private void SetRepeat(uint inDuration)
        {
            if (m_TriggerData != null)
            {
                m_TriggerData.RepeatDuration = (int) inDuration;
                m_TriggerData.OnceLevel = PersistenceLevel.Untracked;
            }
        }

        [BlockMeta("tags"), Preserve]
        private void SetTags(StringSlice inTags)
        {
            foreach(var tag in inTags.EnumeratedSplit(Parsing.CommaChar, StringSplitOptions.RemoveEmptyEntries))
                m_Tags.Add(tag.Trim());
        }

        #endregion // Parser
    }
    
    [Flags]
    public enum ScriptNodeFlags
    {
        Cutscene = 0x01,
        Entrypoint = 0x02,
        TriggerResponse = 0x04,
        Important = 0x08
    }
}