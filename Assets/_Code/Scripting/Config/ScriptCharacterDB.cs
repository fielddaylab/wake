using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Script Character Database")]
    public class ScriptCharacterDB : DBObjectCollection<ScriptCharacterDef>
    {
        #region Inspector

        [SerializeField, Required] private ScriptCharacterDef m_NullActorDefinition = null;
        [SerializeField, Required] private Sprite m_ErrorPortrait = null;

        #endregion // Inspector

        [NonSerialized] private Dictionary<StringHash32, ScriptCharacterDef> m_ActorDefinitionMap;

        public ScriptCharacterDef Default() { return m_NullActorDefinition; }
        public Sprite ErrorPortrait() { return m_ErrorPortrait; }

        protected override ScriptCharacterDef NullValue()
        {
            return m_NullActorDefinition;
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(ScriptCharacterDB))]
        private class Inspector : BaseInspector
        {
        }

        #endif // UNITY_EDITOR
    }
}