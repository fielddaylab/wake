using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using UnityEditorInternal;

namespace Aqua.Editor
{
    // [CustomEditor(typeof(JobDesc))]
    public class JobDescEditor : UnityEditor.Editor {
        private SerializedProperty m_CategoryProperty;
        private SerializedProperty m_FlagsProperty;
        
        private SerializedProperty m_NameIdProperty;
        private SerializedProperty m_PosterIdProperty;
        private SerializedProperty m_DescIdProperty;
        private SerializedProperty m_DescShortIdProperty;
        private SerializedProperty m_DescCompletedIdProperty;

        private SerializedProperty m_ExperimentDifficultyProperty;
        private SerializedProperty m_ModelingDifficultyProperty;
        private SerializedProperty m_ArgumentationDifficultyProperty;

        private SerializedProperty m_PrerequisiteJobsProperty;
        private SerializedProperty m_PrereqConditionsProperty;

        private SerializedProperty m_StationIdProperty;
        private SerializedProperty m_DiveSiteIdsProperty;

        private SerializedProperty m_TasksProperty;

        private SerializedProperty m_CashRewardProperty;
        private SerializedProperty m_GearRewardProperty;
        private SerializedProperty m_AdditionalRewardsProperty;

        private SerializedProperty m_ScriptingProperty;
        private SerializedProperty m_ExtraAssetsProperty;

        private ReorderableList m_PrerequisiteJobsList;
        private ReorderableList m_DiveSiteIdsList;
        private ReorderableList m_TasksList;
        private ReorderableList m_AdditionalRewardsList;
        private ReorderableList m_ExtraAssetsList;

        [SerializeField] private bool m_TextExpanded = true;
        [SerializeField] private bool m_PrerequisitesExpanded = true;
        [SerializeField] private bool m_LocationsExpanded = true;
        [SerializeField] private bool m_RewardsExpanded = false;
        [SerializeField] private bool m_AssetsExpanded = true;
        [SerializeField] private bool m_DifficultyExpanded = false;
        [SerializeField] private bool m_TasksExpanded = true;

        private void OnEnable() {
            m_CategoryProperty = serializedObject.FindProperty("m_Category");
            m_FlagsProperty = serializedObject.FindProperty("m_Flags");
            m_NameIdProperty = serializedObject.FindProperty("m_NameId");
            m_PosterIdProperty = serializedObject.FindProperty("m_PosterId");
            m_DescIdProperty = serializedObject.FindProperty("m_DescId");
            m_DescShortIdProperty = serializedObject.FindProperty("m_DescShortId");
            m_DescCompletedIdProperty = serializedObject.FindProperty("m_DescCompletedId");
            m_ExperimentDifficultyProperty = serializedObject.FindProperty("m_ExperimentDifficulty");
            m_ModelingDifficultyProperty = serializedObject.FindProperty("m_ModelingDifficulty");
            m_ArgumentationDifficultyProperty = serializedObject.FindProperty("m_ArgumentationDifficulty");
            m_PrerequisiteJobsProperty = serializedObject.FindProperty("m_PrerequisiteJobs");
            m_PrereqConditionsProperty = serializedObject.FindProperty("m_PrereqConditions");
            m_StationIdProperty = serializedObject.FindProperty("m_StationId");
            m_DiveSiteIdsProperty = serializedObject.FindProperty("m_DiveSiteIds");
            m_TasksProperty = serializedObject.FindProperty("m_Tasks");
            m_CashRewardProperty = serializedObject.FindProperty("m_CashReward");
            m_GearRewardProperty = serializedObject.FindProperty("m_GearReward");
            m_AdditionalRewardsProperty = serializedObject.FindProperty("m_AdditionalRewards");
            m_ScriptingProperty = serializedObject.FindProperty("m_Scripting");
            m_ExtraAssetsProperty = serializedObject.FindProperty("m_ExtraAssets");

            m_PrerequisiteJobsList = new ReorderableList(serializedObject, m_PrerequisiteJobsProperty);
            m_PrerequisiteJobsList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Jobs");
            m_PrerequisiteJobsList.drawElementCallback = DefaultElementDelegate(m_PrerequisiteJobsList);

            m_DiveSiteIdsList = new ReorderableList(serializedObject, m_DiveSiteIdsProperty);
            m_DiveSiteIdsList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Dive Sites");
            m_DiveSiteIdsList.drawElementCallback = DefaultElementDelegate(m_DiveSiteIdsList);

            m_TasksList = new ReorderableList(serializedObject, m_TasksProperty);
            m_TasksList.drawHeaderCallback = (r) => { };
            m_TasksList.headerHeight = 0;
            m_TasksList.drawElementCallback = DrawTaskCallback;

            m_AdditionalRewardsList = new ReorderableList(serializedObject, m_AdditionalRewardsProperty);
            m_AdditionalRewardsList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Additional Rewards");
            m_AdditionalRewardsList.drawElementCallback = DefaultElementDelegate(m_AdditionalRewardsList);

            m_ExtraAssetsList = new ReorderableList(serializedObject, m_ExtraAssetsProperty);
            m_ExtraAssetsList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Extra Assets");
            m_ExtraAssetsList.drawElementCallback = DefaultElementDelegate(m_ExtraAssetsList);
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_CategoryProperty);
            EditorGUILayout.PropertyField(m_FlagsProperty);

            if (Section("Text", ref m_TextExpanded)) {
                EditorGUILayout.PropertyField(m_NameIdProperty);
                EditorGUILayout.PropertyField(m_PosterIdProperty);
                EditorGUILayout.PropertyField(m_DescIdProperty);
                EditorGUILayout.PropertyField(m_DescShortIdProperty);
                EditorGUILayout.PropertyField(m_DescCompletedIdProperty);
            }

            if (Section("Prerequisites", ref m_PrerequisitesExpanded)) {
                m_PrerequisiteJobsList.DoLayoutList();
                EditorGUILayout.PropertyField(m_PrereqConditionsProperty);
            }

            if (Section("Locations", ref m_LocationsExpanded)) {
                EditorGUILayout.PropertyField(m_StationIdProperty);
                m_DiveSiteIdsList.DoLayoutList();
            }

            if (Section("Tasks", ref m_TasksExpanded)) {
                m_TasksList.DoLayoutList();
            }

            if (Section("Rewards", ref m_RewardsExpanded)) {
                EditorGUILayout.PropertyField(m_CashRewardProperty);
                EditorGUILayout.PropertyField(m_GearRewardProperty);
                m_AdditionalRewardsList.DoLayoutList();
            }

            if (Section("Assets", ref m_AssetsExpanded)) {
                EditorGUILayout.PropertyField(m_ScriptingProperty);
                m_ExtraAssetsList.DoLayoutList();
            }

            if (Section("Difficulty Ratings", ref m_DifficultyExpanded)) {
                EditorGUILayout.PropertyField(m_ExperimentDifficultyProperty);
                EditorGUILayout.PropertyField(m_ModelingDifficultyProperty);
                EditorGUILayout.PropertyField(m_ArgumentationDifficultyProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTaskCallback(Rect rect, int index, bool isActive, bool isFocused) {

        }

        private void TaskHeightCallback(int index) {

        }

        static private void Header(string inHeader) {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(inHeader, EditorStyles.boldLabel);
        }

        static private bool Section(string inHeader, ref bool ioState) {
            EditorGUILayout.Space();
            ioState = EditorGUILayout.Foldout(ioState, inHeader, EditorStyles.foldoutHeader);
            return ioState;
        }

        static private ReorderableList.ElementCallbackDelegate DefaultElementDelegate(ReorderableList list) {
            return (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };
        }

        static private ReorderableList.ElementHeightCallbackDelegate DefaultHeightDelegate(ReorderableList list) {
            return (int index) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, null, true);
            };
        }
    }
}