using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using UnityEditorInternal;

namespace Aqua.Editor
{
    [CustomEditor(typeof(LocText)), CanEditMultipleObjects]
    public class LocTextEditor : UnityEditor.Editor {
        private SerializedProperty m_DefaultTextProperty;
        
        private void OnEnable() {
            m_DefaultTextProperty = serializedObject.FindProperty("m_DefaultText");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_DefaultTextProperty);
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                foreach(LocText text in targets) {
                    string val;
                    if (LocEditor.TryLookup(text.m_DefaultText.ToDebugString(), out val)) {
                        Undo.RecordObject(text.Graphic, "Set Text");
                        text.Graphic.text = val;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}