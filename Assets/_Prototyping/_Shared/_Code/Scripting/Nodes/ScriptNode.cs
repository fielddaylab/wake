using System;
using BeauUtil.Blocks;
using System.Collections.Generic;

namespace ProtoAqua
{
    public class ScriptNode : IDataBlock
    {
        static public readonly string NullId = "[null]";

        #region Serialized

        // Ids
        private string m_SelfId = null;
        private string m_FullId = null;

        // Properties
        private ScriptNodeFlags m_Flags = 0;
        private ScriptNodePackage m_Package = null;

        // Text
        private List<string> m_Lines = new List<string>();

        #endregion // Serialized

        public ScriptNode(ScriptNodePackage inPackage, string inSelfId, string inFullId)
        {
            m_Package = inPackage;
            m_SelfId = inSelfId;
            m_FullId = inFullId;
        }

        public string Id() { return m_FullId; }
        public string SelfId() { return m_SelfId; }
        public ScriptNodeFlags Flags() { return m_Flags; }
        public ScriptNodePackage Package() { return m_Package; }

        public IReadOnlyList<string> Lines() { return m_Lines; }

        #region Parser

        [BlockContent(BlockContentMode.LineByLine)]
        private void AddContent(string inLine)
        {
            m_Lines.Add(inLine);
        }

        [BlockMeta("cutscene")]
        private void SetCutscene(bool inbCutscene = true)
        {
            if (inbCutscene)
                m_Flags |= ScriptNodeFlags.Cutscene;
            else
                m_Flags &= ~ScriptNodeFlags.Cutscene;
        }

        [BlockMeta("entrypoint")]
        private void SetEntrypoint()
        {
            m_Flags |= ScriptNodeFlags.Entrypoint;
        }

        #endregion // Parser
    }
    
    [Flags]
    public enum ScriptNodeFlags
    {
        Cutscene = 0x01,
        Entrypoint = 0x02
    }
}