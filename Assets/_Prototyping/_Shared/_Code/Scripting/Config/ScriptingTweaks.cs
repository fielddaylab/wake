using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;

namespace ProtoAqua
{
    [CreateAssetMenu(menuName = "Prototype/Scripting Tweaks")]
    public class ScriptingTweaks : TweakAsset
    {
        #region Inspector

        [SerializeField, Required] private ScriptActorDefinition[] m_ActorDefinitions = null;
        [SerializeField, Required] private ScriptActorDefinition m_NullActorDefinition = null;
        [SerializeField] private float m_CutsceneEndNextTriggerDelay = 0.2f;

        #endregion // Inspector

        [NonSerialized] private Dictionary<StringHash32, ScriptActorDefinition> m_ActorDefinitionMap;

        public float CutsceneEndNextTriggerDelay() { return m_CutsceneEndNextTriggerDelay; }

        public ScriptActorDefinition ActorDef(StringHash32 inId)
        {
            if (inId.IsEmpty)
                return m_NullActorDefinition;

            ScriptActorDefinition actorDef;
            if (!GetActorDefMap().TryGetValue(inId, out actorDef))
            {
                Debug.LogErrorFormat("[ScriptingTweaks] No ScriptActorDefinition found with id '{0}'", inId.ToDebugString());
                actorDef = m_NullActorDefinition;
            }

            return actorDef;
        }

        private Dictionary<StringHash32, ScriptActorDefinition> GetActorDefMap()
        {
            if (m_ActorDefinitionMap == null)
            {
                m_ActorDefinitionMap = KeyValueUtils.CreateMap<StringHash32, ScriptActorDefinition, ScriptActorDefinition>(m_ActorDefinitions);
            }

            return m_ActorDefinitionMap;
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            ValidationUtils.EnsureUnique(ref m_ActorDefinitions);
        }

        [ContextMenu("Load All In Directory")]
        private void FindAllDefinitions()
        {
            m_ActorDefinitions = ValidationUtils.FindAllAssets<ScriptActorDefinition>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.CustomEditor(typeof(ScriptingTweaks))]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Load All Actor Definitions"))
                {
                    foreach(ScriptingTweaks tweaks in targets)
                    {
                        tweaks.FindAllDefinitions();
                    }
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}