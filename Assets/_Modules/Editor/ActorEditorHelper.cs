#if UNITY_EDITOR

using System;
using Aqua;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using ScriptableBake;
using UnityEditor;
using UnityEngine;

namespace ProtoAqua.ExperimentV2.Editor {
    
    public class ActorEditorHelper : EditorWindow {
        [MenuItem("Aqualab/Open Actor Editor")]
        static private void MenuOpen() {
            GetWindow<ActorEditorHelper>().Show();
        }

        [SerializeField] public ActorInstance Instance;
        
        [NonSerialized] public int DefinitionIndex;
        [NonSerialized] public ActorDefinitions Definitions;

        private SerializedObject m_CritterDefinitionsAsset;
        private SerializedProperty m_CritterDefinitionProp;
        private SerializedProperty m_SelectedDefinition;
        [SerializeField] private Vector2 m_Scroll;
        [SerializeField] private bool m_ShowDebug;

        static private GUIStyle BoldFoldout;

        private void OnEnable() {
            titleContent = new GUIContent("Experimentation Actor Editor");
            Definitions = ValidationUtils.FindAsset<ActorDefinitions>();
            m_CritterDefinitionsAsset = new SerializedObject(Definitions);
            m_CritterDefinitionProp = m_CritterDefinitionsAsset.FindProperty("CritterDefinitions");
            Selection.selectionChanged += UpdateFromSelection;
            UpdateFromSelection();
        }

        private void OnDisable() {
            Selection.selectionChanged -= UpdateFromSelection;
        }

        private void OnGUI() {
            if (BoldFoldout == null) {
                BoldFoldout = new GUIStyle(EditorStyles.foldout);
                BoldFoldout.fontStyle = EditorStyles.boldLabel.fontStyle;
            }

            m_CritterDefinitionsAsset.UpdateIfRequiredOrScript();

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.ExpandWidth(true));
            using(new GUIScopes.IndentLevelScope(1))
            using(new GUIScopes.LabelWidthScope(300)) {
                if (Instance != null && m_SelectedDefinition != null) {
                    string name = Instance.gameObject.name;
                    EditorGUILayout.LabelField("Definition:", name, EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    RenderInline(m_SelectedDefinition.Copy(), Instance, ref m_ShowDebug);
                }

                if (GUILayout.Button("Bake Actor Definitions")) {
                    using(Profiling.Time("bake assets"))
                    {
                        using(Log.DisableMsgStackTrace())
                        {
                            Baking.BakeObjects(new UnityEngine.Object[] { Definitions }, BakeFlags.Verbose | BakeFlags.ShowProgressBar);
                        }
                    }
                    using(Profiling.Time("post-bake save assets"))
                    {
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            m_CritterDefinitionsAsset.ApplyModifiedProperties();
        }

        static private void RenderInline(SerializedProperty property, ActorInstance instance, ref bool showDebug) {
            UnityEditor.SerializedProperty endProperty = property.GetEndProperty();

            property.NextVisible(true);
            property.NextVisible(false);

            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("Spawning", EditorStyles.boldLabel);
                NextRender(property);
                Next(property);
                Next(property, true);
                Render(property);
                NextRender(property);
                NextRender(property);
                NextRender(property);
            }

            EditorGUILayout.Space();

            ActorDefinition.MovementTypeId moveType;
            ActorDefinition.EatTypeId eatType;

            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("Motion", EditorStyles.boldLabel);
                Next(property);
                Next(property, true);
                Render(property);
                moveType = (ActorDefinition.MovementTypeId) property.intValue;
                NextRender(property, moveType != 0);
                NextRender(property, moveType != 0);
                NextRender(property, moveType != 0);
                NextRender(property, moveType != 0);
                NextRender(property, moveType != 0);
            }

            EditorGUILayout.Space();

            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("Eating", EditorStyles.boldLabel);
                Next(property);
                Next(property, true);
                Render(property);
                eatType = (ActorDefinition.EatTypeId) property.intValue;
                NextRender(property, moveType != 0 && eatType != 0);
            }

            EditorGUILayout.Space();

            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("Stressed", EditorStyles.boldLabel);
                Next(property);
                Next(property, true);
                if (moveType != 0) {
                    Render(property);
                }
                NextRender(property, moveType != 0);
                NextRender(property, instance.IdleAnimation);
            }

            EditorGUILayout.Space();

            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                using(new GUIScopes.IndentLevelScope(-1)) {
                    showDebug = EditorGUILayout.Foldout(showDebug, "Debug", BoldFoldout);
                }
                if (showDebug) {
                    while(Next(property, false, endProperty)) {
                        Render(property);
                    }
                }
            }
        }

        static private bool Next(SerializedProperty property, bool children = false, SerializedProperty end = null) {
            if (end != null) {
                return property.NextVisible(children) && !SerializedProperty.EqualContents(property, end);
            }
            return property.NextVisible(children);
        }

        static private void NextRender(SerializedProperty property, bool render = true) {
            Next(property);
            if (render) {
                Render(property);
            }
        }

        static private void Render(SerializedProperty property) {
            property.isExpanded = true;
            UnityEditor.EditorGUILayout.PropertyField(property, true);
        }

        private void UpdateFromSelection() {
            GameObject go = Selection.activeGameObject;
            if (go != null) {
                var instance = go.GetComponent<ActorInstance>();
                if (instance != null) {
                    Instance = instance;
                    UpdateSelectedDefinition();
                }
            }
        }

        private void UpdateSelectedDefinition() {
            if (!Instance) {
                return;
            }

            DefinitionIndex = FindDefinition(Definitions, Instance.gameObject.name);
            if (DefinitionIndex >= 0) {
                m_SelectedDefinition = m_CritterDefinitionProp.GetArrayElementAtIndex(DefinitionIndex);
                Repaint();
            }
        }

        static private int FindDefinition(ActorDefinitions definitions, StringHash32 id) {
            for(int i = 0; i < definitions.CritterDefinitions.Length; i++) {
                if (definitions.CritterDefinitions[i].Id == id) {
                    return i;
                }
            }

            return -1;
        }
    }

}

#endif // UNITY_EDITOR