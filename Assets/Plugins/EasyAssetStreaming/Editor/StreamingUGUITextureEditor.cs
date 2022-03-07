using UnityEngine;
using UnityEditor;

namespace EasyAssetStreaming.Editor {
    [CustomEditor(typeof(StreamingUGUITexture)), CanEditMultipleObjects]
    public sealed class StreamingUGUITextureEditor : UnityEditor.Editor {

        private SerializedProperty m_PathProperty;
        private SerializedProperty m_UVRectProperty;
        private SerializedProperty m_AutoSizeProperty;
        private SerializedProperty m_VisibleProperty;

        private void OnEnable() {
            m_PathProperty = serializedObject.FindProperty("m_Path");
            m_UVRectProperty = serializedObject.FindProperty("m_UVRect");
            m_AutoSizeProperty = serializedObject.FindProperty("m_AutoSize");
            m_VisibleProperty = serializedObject.FindProperty("m_Visible");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_PathProperty);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_VisibleProperty);
            EditorGUILayout.PropertyField(m_UVRectProperty);
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(m_AutoSizeProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}