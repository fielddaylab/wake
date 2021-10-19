using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using UnityEditorInternal;
using BeauUtil;
using System;
using System.Reflection;
using Leaf;
using System.IO;

namespace Aqua.Editor
{
    [CustomEditor(typeof(JobDesc)), CanEditMultipleObjects]
    public class JobDescEditor : UnityEditor.Editor {
        private const string ScriptTemplateAssetPath = "Assets/Editor/JobScriptTemplate.template.leaf";
        private const string ScriptTemplateJobNameReplaceKey = "[[JOB-NAME]]";

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
        private ReorderableList m_TaskPrerequisiteList;
        private ReorderableList m_TaskStepList;

        private NamedItemList<SerializedHash32> m_TaskSelectorList;
        private NamedItemList<StringHash32> m_TaskStepFactSelectorList = new NamedItemList<StringHash32>();
        private NamedItemList<StringHash32> m_TaskStepBestiarySelectorList = new NamedItemList<StringHash32>();
        private NamedItemList<StringHash32> m_TaskStepStationSelectorList = new NamedItemList<StringHash32>();
        private NamedItemList<StringHash32> m_TaskStepItemSelectorList = new NamedItemList<StringHash32>();

        [SerializeField] private bool m_TextExpanded = true;
        [SerializeField] private bool m_PrerequisitesExpanded = true;
        [SerializeField] private bool m_LocationsExpanded = true;
        [SerializeField] private bool m_RewardsExpanded = false;
        [SerializeField] private bool m_AssetsExpanded = true;
        [SerializeField] private bool m_DifficultyExpanded = false;
        [SerializeField] private bool m_TasksExpanded = true;

        [SerializeField] private Vector2 m_TaskListScroll;
        [SerializeField] private Vector2 m_TaskSettingsScroll;
        [SerializeField] private int m_SelectedTaskIdx = -1;

        [NonSerialized] static private GUIContent s_TempContent;

        [NonSerialized] private double m_JobStepFactSelectorLastUpdate;
        [NonSerialized] private double m_JobStepBestiarySelectorLastUpdate;
        [NonSerialized] private double m_JobStepStationSelectorLastUpdate;
        [NonSerialized] private double m_JobStepItemSelectorLastUpdate;

        static private readonly FieldInfo JobStepTargetFieldInfo = typeof(JobStep).GetField("Target");
        static private readonly FactIdAttribute JobStepFactSelector = new FactIdAttribute();
        static private readonly FilterBestiaryIdAttribute JobStepBestiarySelector = new FilterBestiaryIdAttribute();
        static private readonly MapIdAttribute JobStepStationSelector = new MapIdAttribute(MapCategory.Station);
        static private readonly ItemIdAttribute JobStepItemSelector = new ItemIdAttribute();

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
            m_TasksList.headerHeight = 0;
            m_TasksList.drawHeaderCallback = (r) => { };
            m_TasksList.showDefaultBackground = false;
            m_TasksList.drawElementCallback = RenderTaskListElement;
            m_TasksList.footerHeight = 0;
            m_TasksList.drawFooterCallback = (r) => { };

            m_AdditionalRewardsList = new ReorderableList(serializedObject, m_AdditionalRewardsProperty);
            m_AdditionalRewardsList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Additional Rewards");
            m_AdditionalRewardsList.drawElementCallback = DefaultElementDelegate(m_AdditionalRewardsList);

            m_ExtraAssetsList = new ReorderableList(serializedObject, m_ExtraAssetsProperty);
            m_ExtraAssetsList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Extra Assets");
            m_ExtraAssetsList.drawElementCallback = DefaultElementDelegate(m_ExtraAssetsList);

            m_TaskSelectorList = new NamedItemList<SerializedHash32>();
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
                if (targets.Length > 1) {
                    EditorGUILayout.HelpBox("Task lists cannot be edited while multiple jobs are selected", MessageType.Warning);
                } else {
                    JobDesc desc = (JobDesc) targets[0];
                    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(300));
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(200));
                    m_TaskListScroll = EditorGUILayout.BeginScrollView(m_TaskListScroll, GUILayout.ExpandHeight(true));
                    m_SelectedTaskIdx = RenderTaskList(desc, m_SelectedTaskIdx);
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Create Task")) {
                        m_TasksProperty.arraySize++;
                        m_SelectedTaskIdx = m_TasksProperty.arraySize - 1;
                        serializedObject.ApplyModifiedProperties();
                    }
                    using(new EditorGUI.DisabledScope(m_SelectedTaskIdx >= 0 && m_TasksProperty.arraySize <= 1)) {
                        if (GUILayout.Button("Delete Task")) {
                            m_TasksProperty.DeleteArrayElementAtIndex(m_SelectedTaskIdx);
                            if (m_SelectedTaskIdx >= m_TasksProperty.arraySize - 1) {
                                m_SelectedTaskIdx = m_TasksProperty.arraySize - 1;
                            }
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();

                    m_TaskSettingsScroll = EditorGUILayout.BeginScrollView(m_TaskSettingsScroll, EditorStyles.helpBox);
                    RenderTaskSettings(desc, m_SelectedTaskIdx);
                    EditorGUILayout.EndScrollView();
                    
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (Section("Rewards", ref m_RewardsExpanded)) {
                EditorGUILayout.PropertyField(m_CashRewardProperty);
                EditorGUILayout.PropertyField(m_GearRewardProperty);
                m_AdditionalRewardsList.DoLayoutList();
            }

            if (Section("Assets", ref m_AssetsExpanded)) {
                EditorGUILayout.PropertyField(m_ScriptingProperty);
                m_ExtraAssetsList.DoLayoutList();

                EditorGUILayout.Space();

                using(new EditorGUI.DisabledScope(m_ScriptingProperty.hasMultipleDifferentValues || m_ScriptingProperty.objectReferenceValue != null)) {
                    if (GUILayout.Button("Create Script From Template")) {
                        foreach(JobDesc job in targets) {
                            Undo.RecordObject(job, "Creating Script");
                            EditorUtility.SetDirty(job);
                            job.m_Scripting = GenerateBaseScript(job);
                        }
                    }
                }
            }

            if (Section("Difficulty Ratings", ref m_DifficultyExpanded)) {
                EditorGUILayout.PropertyField(m_ExperimentDifficultyProperty);
                EditorGUILayout.PropertyField(m_ModelingDifficultyProperty);
                EditorGUILayout.PropertyField(m_ArgumentationDifficultyProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private int RenderTaskList(JobDesc inJob, int inIndex) {
            m_TasksList.index = inIndex;
            m_TasksList.DoLayoutList();
            return m_TasksList.index;
        }

        private void RenderTaskListElement(Rect rect, int index, bool isActive, bool isFocused) {
            JobDesc job = (JobDesc) target;
            JobDesc.EditorJobTask jobTask = job.m_Tasks[index];
            EditorGUI.LabelField(rect, jobTask.Id.ToDebugString());
        }

        private void RenderTaskSettings(JobDesc inJob, int inIndex) {
            if (inIndex < 0 || inIndex >= inJob.m_Tasks.Length) {
                EditorGUILayout.LabelField("No task selected", EditorStyles.boldLabel);
            } else {
                using(new GUIScopes.LabelWidthScope(100)) {
                    SerializedProperty taskAsProperty = m_TasksProperty.GetArrayElementAtIndex(inIndex);
                    JobDesc.EditorJobTask taskObject = inJob.m_Tasks[inIndex];

                    SerializedProperty idProperty = taskAsProperty.FindPropertyRelative("Id");
                    SerializedProperty labelProperty = taskAsProperty.FindPropertyRelative("LabelId");
                    EditorGUILayout.PropertyField(idProperty);
                    EditorGUILayout.PropertyField(labelProperty);

                    EditorGUILayout.Space();

                    SerializedProperty prereqProperty = taskAsProperty.FindPropertyRelative("PrerequisiteTaskIds");
                    if (m_TaskPrerequisiteList == null) {
                        m_TaskPrerequisiteList = new ReorderableList(serializedObject, prereqProperty);
                        m_TaskPrerequisiteList.drawHeaderCallback = (r) => GUI.Label(r, "Prerequisite Tasks");
                        m_TaskPrerequisiteList.drawElementCallback = RenderTaskPrerequisiteSelector(m_TaskPrerequisiteList);
                    }

                    GenerateTaskList(inJob, taskObject.Id);
                    m_TaskPrerequisiteList.serializedProperty = prereqProperty;

                    m_TaskPrerequisiteList.DoLayoutList();

                    EditorGUILayout.Space();

                    SerializedProperty stepsProperty = taskAsProperty.FindPropertyRelative("Steps");
                    if (m_TaskStepList == null) {
                        m_TaskStepList = new ReorderableList(serializedObject, stepsProperty);
                        m_TaskStepList.drawHeaderCallback = (r) => GUI.Label(r, "Steps");
                        m_TaskStepList.drawElementCallback = RenderTaskStep(m_TaskStepList);
                        m_TaskStepList.elementHeightCallback = TaskStepHeight(m_TaskStepList);
                    }

                    m_TaskStepList.serializedProperty = stepsProperty;
                    m_TaskStepList.DoLayoutList();
                }
            }
        }

        private ReorderableList.ElementCallbackDelegate RenderTaskPrerequisiteSelector(ReorderableList list) {
            return (Rect rect, int index, bool isActive, bool isFocused) => {
                JobDesc.EditorJobTask task = ((JobDesc) target).m_Tasks[m_SelectedTaskIdx];
                SerializedHash32 obj = task.PrerequisiteTaskIds[index];
                SerializedHash32 next = ListGUI.Popup(rect, obj, m_TaskSelectorList);
                if (next.Hash() != obj.Hash()) {
                    Undo.RecordObject(target, "Changing prerequisite");
                    task.PrerequisiteTaskIds[index] = next;
                }
            };
        }

        private NamedItemList<SerializedHash32> GenerateTaskList(JobDesc inJob, StringHash32 inExclude = default) {
            var list = m_TaskSelectorList;
            list.Clear();

            int taskIdx = 0;
            foreach(var task in inJob.m_Tasks) {
                if (task.Id == inExclude) {
                    continue;
                }

                list.Add(task.Id, task.Id.ToDebugString(), taskIdx++);
            }

            return list;
        }

        private ReorderableList.ElementCallbackDelegate RenderTaskStep(ReorderableList list) {
            return (Rect rect, int index, bool isActive, bool isFocused) => {
                Rect line = rect;
                line.height = EditorGUIUtility.singleLineHeight;
                line.y += 4;

                SerializedProperty stepProp = list.serializedProperty.GetArrayElementAtIndex(index);
                stepProp.Next(true);

                // type

                EditorGUI.PropertyField(line, stepProp);

                JobStepType stepType = (JobStepType) stepProp.intValue;
                
                line.y += EditorGUIUtility.singleLineHeight + 2;

                switch(stepType) {
                    case JobStepType.ScanObject:
                    case JobStepType.SeeScriptNode: {
                        stepProp.Next(false);
                        EditorGUI.PropertyField(line, stepProp, TempContent("Scan Id"));
                        break;
                    }

                    case JobStepType.AddFactToModel:
                    case JobStepType.AcquireFact:
                    case JobStepType.UpgradeFact: {
                        stepProp.Next(false);
                        FactIdPropertyDrawer.Render(line, stepProp, TempContent("Fact Id"), JobStepTargetFieldInfo, JobStepFactSelector, ref m_JobStepFactSelectorLastUpdate, m_TaskStepFactSelectorList);
                        break;
                    }

                    case JobStepType.AcquireBestiaryEntry: {
                        stepProp.Next(false);
                        DBObjectIdPropertyDrawer.Render(line, stepProp, TempContent("Bestiary Id"), JobStepTargetFieldInfo, JobStepBestiarySelector, ref m_JobStepBestiarySelectorLastUpdate, m_TaskStepBestiarySelectorList);
                        break;
                    }

                    case JobStepType.GotoStation: {
                        stepProp.Next(false);
                        DBObjectIdPropertyDrawer.Render(line, stepProp, TempContent("Station Id"), JobStepTargetFieldInfo, JobStepStationSelector, ref m_JobStepStationSelectorLastUpdate, m_TaskStepStationSelectorList);
                        break;
                    }

                    case JobStepType.GotoScene: {
                        stepProp.Next(false);
                        EditorGUI.PropertyField(line, stepProp, TempContent("Scene Name"));
                        break;
                    }

                    case JobStepType.EvaluateCondition: {
                        stepProp.Next(false);
                        stepProp.Next(false);
                        EditorGUI.PropertyField(line, stepProp, TempContent("Conditions"));
                        break;
                    }

                    case JobStepType.GetItem: {
                        stepProp.Next(false);
                        DBObjectIdPropertyDrawer.Render(line, stepProp, TempContent("Item Id"), JobStepTargetFieldInfo, JobStepItemSelector, ref m_JobStepItemSelectorLastUpdate, m_TaskStepItemSelectorList);
                        stepProp.Next(false);
                        stepProp.Next(false);
                        line.y += EditorGUIUtility.singleLineHeight + 2;
                        EditorGUI.PropertyField(line, stepProp, TempContent("Amount"));
                        break;
                    }
                }
            };
        }

        private ReorderableList.ElementHeightCallbackDelegate TaskStepHeight(ReorderableList list) {
            return (int index) => {
                SerializedProperty stepProp = list.serializedProperty.GetArrayElementAtIndex(index);
                stepProp.Next(true);

                JobStepType stepType = (JobStepType) stepProp.intValue;

                int numLines = 1;

                switch(stepType) {
                    case JobStepType.GetItem:
                        numLines += 2;
                        break;

                    case JobStepType.FinishArgumentation:
                        break;

                    default:
                        numLines++;
                        break;
                }

                return (EditorGUIUtility.singleLineHeight + 2) * numLines + 8;
            };
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

        static private GUIContent TempContent(string inText) {
            GUIContent c = s_TempContent ?? (s_TempContent = new GUIContent());
            c.text = inText;
            return c;
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
    
        static private LeafAsset GenerateBaseScript(JobDesc inJob) {
            string directory = AssetDatabase.GetAssetPath(inJob);
            string newPath = Path.Combine(Path.GetDirectoryName(directory), "script.leaf");
            if (File.Exists(newPath)) {
                return AssetDatabase.LoadAssetAtPath<LeafAsset>(newPath);
            }

            string file = File.ReadAllText(ScriptTemplateAssetPath);
            file = file.Replace(ScriptTemplateJobNameReplaceKey, inJob.name);
            File.WriteAllText(newPath, file);

            AssetDatabase.ImportAsset(newPath);

            return AssetDatabase.LoadAssetAtPath<LeafAsset>(newPath);
        }
    }
}