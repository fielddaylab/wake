using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using UnityEditorInternal;

namespace Aqua.Editor
{
    [CustomEditor(typeof(LocText)), CanEditMultipleObjects]
    public class LocTextEditor : UnityEditor.Editor {
        private SerializedProperty m_DefaultTextProperty;
        private SerializedProperty m_TintSpritesProperty;
        private SerializedProperty m_PrefixProperty;
        private SerializedProperty m_PostfixProperty;
        
        private void OnEnable() {
            m_DefaultTextProperty = serializedObject.FindProperty("m_DefaultText");
            m_TintSpritesProperty = serializedObject.FindProperty("m_TintSprites");
            m_PrefixProperty = serializedObject.FindProperty("m_Prefix");
            m_PostfixProperty = serializedObject.FindProperty("m_Postfix");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_DefaultTextProperty);
            EditorGUILayout.PropertyField(m_TintSpritesProperty);
            EditorGUILayout.PropertyField(m_PrefixProperty);
            EditorGUILayout.PropertyField(m_PostfixProperty);
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                foreach(LocText text in targets) {
                    string val;
                    if (LocEditor.TryLookup(text.m_DefaultText.ToDebugString(), out val)) {
                        Undo.RecordObject(text.Graphic, "Set Text");
                        text.InternalSetText(val);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}