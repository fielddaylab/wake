using UnityEngine;
using UnityEditor;
using System;

namespace EasyAssetStreaming.Editor {
    [CustomEditor(typeof(StreamingQuadTexture)), CanEditMultipleObjects]
    public sealed class StreamingWorldTextureEditor : UnityEditor.Editor {

        private SerializedProperty m_PathProperty;
        private SerializedProperty m_MaterialProperty;
        private SerializedProperty m_TessellationProperty;
        private SerializedProperty m_ColorProperty;
        private SerializedProperty m_VisibleProperty;

        private SerializedProperty m_SizeProperty;
        private SerializedProperty m_PivotProperty;
        private SerializedProperty m_UVRectProperty;
        private SerializedProperty m_AutoSizeProperty;
        
        private SerializedProperty m_SortingLayerProperty;
        private SerializedProperty m_SortingOrderProperty;

        static private GUIContent[] s_LayerContent;
        static private int[] s_LayerIds;

        static private void InitializeLayerNames() {
            SortingLayer[] layers = SortingLayer.layers;
            Array.Resize(ref s_LayerContent, layers.Length);
            Array.Resize(ref s_LayerIds, layers.Length);

            for (int i = 0; i < layers.Length; ++i)
            {
                s_LayerContent[i] = new GUIContent(layers[i].name);
                s_LayerIds[i] = layers[i].id;
            }
        }

        private void OnEnable() {
            m_PathProperty = serializedObject.FindProperty("m_Path");
            m_MaterialProperty = serializedObject.FindProperty("m_Material");
            m_TessellationProperty = serializedObject.FindProperty("m_Tessellation");
            m_ColorProperty = serializedObject.FindProperty("m_Color");
            m_VisibleProperty = serializedObject.FindProperty("m_Visible");

            m_SizeProperty = serializedObject.FindProperty("m_Size");
            m_PivotProperty = serializedObject.FindProperty("m_Pivot");
            m_UVRectProperty = serializedObject.FindProperty("m_UVRect");
            m_AutoSizeProperty = serializedObject.FindProperty("m_AutoSize");

            m_SortingLayerProperty = serializedObject.FindProperty("m_SortingLayer");
            m_SortingOrderProperty = serializedObject.FindProperty("m_SortingOrder");

            InitializeLayerNames();
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_PathProperty);
            EditorGUILayout.PropertyField(m_MaterialProperty);
            EditorGUILayout.PropertyField(m_TessellationProperty);
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

            Rect sortingLayerRect = EditorGUILayout.BeginHorizontal();
            GUIContent sortingLayerLabel = EditorGUI.BeginProperty(sortingLayerRect, new GUIContent(m_SortingLayerProperty.displayName), m_SortingLayerProperty);
            {
                int layerIdx = m_SortingLayerProperty.hasMultipleDifferentValues ? -1 : Array.IndexOf(s_LayerIds, m_SortingLayerProperty.intValue);

                UnityEditor.EditorGUI.BeginChangeCheck();
                int nextLayerIdx = UnityEditor.EditorGUILayout.Popup(sortingLayerLabel, layerIdx, s_LayerContent);
                if (UnityEditor.EditorGUI.EndChangeCheck())
                    m_SortingLayerProperty.intValue = nextLayerIdx >= 0 ? s_LayerIds[nextLayerIdx] : -1;
            }
            EditorGUI.EndProperty();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(m_SortingOrderProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}