using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Aqua.Editor {
    [CustomEditor(typeof(AppearAnimSet)), CanEditMultipleObjects]
    public class AppearAnimSetEditor : UnityEditor.Editor {
        private ReorderableList m_AnimsList;
        private SerializedProperty m_AnimsProperty;
        private SerializedProperty m_IntervalScaleProperty;
        private SerializedProperty m_InitialDelayProperty;
        private SerializedProperty m_PlayOnEnableProperty;
        private SerializedProperty m_ClippingRegionProperty;
        private SerializedProperty m_NextSetProperty;

        private void OnEnable() {
            m_AnimsProperty = serializedObject.FindProperty("m_Anims");
            m_IntervalScaleProperty = serializedObject.FindProperty("m_IntervalScale");
            m_InitialDelayProperty = serializedObject.FindProperty("m_InitialDelay");
            m_PlayOnEnableProperty = serializedObject.FindProperty("m_PlayOnEnable");
            m_ClippingRegionProperty = serializedObject.FindProperty("m_ClippingRegion");
            m_NextSetProperty = serializedObject.FindProperty("m_NextSet");

            m_AnimsList = ReorderableUtils.CreateList(serializedObject, m_AnimsProperty, "Animations");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            m_AnimsList.DoLayoutList();

            if (GUILayout.Button("Find All")) {
                foreach(AppearAnimSet set in targets) {
                    set.LoadAll();
                }
            }

            EditorGUILayout.PropertyField(m_IntervalScaleProperty);
            EditorGUILayout.PropertyField(m_InitialDelayProperty);

            EditorGUILayout.PropertyField(m_PlayOnEnableProperty);
            EditorGUILayout.PropertyField(m_ClippingRegionProperty);
            EditorGUILayout.PropertyField(m_NextSetProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}