using System;
using BeauUtil.Blocks;
using System.Collections.Generic;
using UnityEngine.Scripting;
using BeauUtil;
using BeauPools;
using BeauUtil.Variants;
using UnityEngine;
using Leaf;

namespace ProtoAqua.Scripting
{
    public class ScriptNode : LeafNode
    {
        #region Serialized

        // Properties
        private ScriptNodeFlags m_Flags = 0;
        private ScriptNodePackage m_Package = null;
        private TriggerNodeData m_TriggerData = null;
        private HashSet<StringHash32> m_Tags = new HashSet<StringHash32>();

        #endregion // Serialized

        public ScriptNode(ScriptNodePackage inPackage, StringHash32 inFullId)
        {
            m_Package = inPackage;
            m_Id = inFullId;
        }

        public ScriptNodeFlags Flags() { return m_Flags; }
        public ScriptNodePackage Package() { return m_Package; }

        public TriggerNodeData TriggerData { get { return m_TriggerData; } }
        public IReadOnlyCollection<StringHash32> Tags() { return m_Tags; }

        public override ILeafModule Module()
        {
            return m_Package;
        }

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

        public StringHash32 TargetId()
        {
            return m_TriggerData != null ? m_TriggerData.TargetId : StringHash32.Null;
        }

        public TriggerPriority Priority()
        {
            if ((m_Flags & ScriptNodeFlags.Cutscene) != 0)
                return TriggerPriority.Cutscene;

            if (m_TriggerData != null)
            {
                return m_TriggerData.TriggerPriority;
            }

            return TriggerPriority.Low;
        }

        #region Parser

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

        [BlockMeta("chatter"), Preserve]
        private void SetChatter()
        {
            m_Flags |= ScriptNodeFlags.CornerChatter;
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

            if (inTriggerId == GameTriggers.RequestPartnerHelp)
            {
                m_TriggerData.TargetId = "kevin";
            }
        }

        [BlockMeta("who"), Preserve]
        private void SetTriggerTarget(StringHash32 inTargetId)
        {
            if (m_TriggerData != null)
            {
                m_TriggerData.TargetId = inTargetId;
            }
        }

        [BlockMeta("triggerPriority"), Preserve]
        private void SetTriggerPriority(TriggerPriority inPriority)
        {
            if (m_TriggerData != null)
            {
                m_TriggerData.TriggerPriority = inPriority;
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
        Important = 0x08,
        CornerChatter = 0x10,
    }
}