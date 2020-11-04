using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;

namespace ProtoAqua
{
    [CreateAssetMenu(menuName = "Prototype/Script Actor Definition")]
    public class ScriptActorDefinition : ScriptableObject, IKeyValuePair<StringHash32, ScriptActorDefinition>
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id = null;
        [SerializeField] private SerializedHash32 m_NameId = null;
        [SerializeField] private SerializedHash32 m_DefaultTypeSFX = null;
        [SerializeField, AutoEnum] private ScriptActorTypeFlags m_Flags = 0;

        #endregion // Inspector

        public StringHash32 Id() { return m_Id; }
        public StringHash32 NameId() { return m_NameId; }
        public StringHash32 DefaultTypeSfx() { return m_DefaultTypeSFX; }
        public ScriptActorTypeFlags Flags() { return m_Flags; }

        public bool HasFlags(ScriptActorTypeFlags inFlags)
        {
            return (m_Flags & inFlags) != 0;
        }

        #region IKeyValuePair

        StringHash32 IKeyValuePair<StringHash32, ScriptActorDefinition>.Key { get { return m_Id; } }

        ScriptActorDefinition IKeyValuePair<StringHash32, ScriptActorDefinition>.Value { get { return this; } }

        #endregion // IKeyValuePair
    }

    [Flags]
    public enum ScriptActorTypeFlags
    {
        IsPlayer = 0x01,
    }
}