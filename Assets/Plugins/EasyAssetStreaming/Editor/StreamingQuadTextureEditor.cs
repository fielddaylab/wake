using UnityEngine;
using UnityEditor;

namespace EasyAssetStreaming.Editor {
    [CustomEditor(typeof(StreamingQuadTexture)), CanEditMultipleObjects]
    public sealed class StreamingWorldTextureEditor : UnityEditor.Editor {

        private SerializedProperty m_PathProperty;
        private SerializedProperty m_MaterialProperty;
        private SerializedProperty m_ColorProperty;
        private SerializedProperty m_VisibleProperty;

        private SerializedProperty m_SizeProperty;
        private SerializedProperty m_PivotProperty;
        private SerializedProperty m_UVRectProperty;
        private SerializedProperty m_AutoSizeProperty;
        
        private SerializedProperty m_SortingLayerProperty;
        private SerializedProperty m_SortingOrderProperty;

        private void OnEnable() {
            m_PathProperty = serializedObject.FindProperty("m_Path");
            m_MaterialProperty = serializedObject.FindProperty("m_Material");
            m_ColorProperty = serializedObject.FindProperty("m_Color");
            m_VisibleProperty = serializedObject.FindProperty("m_Visible");

            m_SizeProperty = serializedObject.FindProperty("m_Size");
            m_PivotProperty = serializedObject.FindProperty("m_Pivot");
            m_UVRectProperty = serializedObject.FindProperty("m_UVRect");
            m_AutoSizeProperty = serializedObject.FindProperty("m_AutoSize");

            m_SortingLayerProperty = serializedObject.FindProperty("m_SortingLayer");
            m_SortingOrderProperty = serializedObject.FindProperty("m_SortingOrder");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_PathProperty);
            EditorGUILayout.PropertyField(m_MaterialProperty);
            EditorGUILayout.PropertyField(m_ColorProperty);
            EditorGUILayout.PropertyField(m_VisibleProperty);

            if (!m_MaterialProperty.hasMultipleDifferentValues && m_MaterialProperty.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("Material property must be assigned", MessageType.Error);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_SizeProperty);
            EditorGUILayout.PropertyField(m_PivotProperty);
            EditorGUILayout.PropertyField(m_UVRectProperty);
            EditorGUILayout.PropertyField(m_AutoSizeProperty);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_SortingLayerProperty);
            EditorGUILayout.PropertyField(m_SortingOrderProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}