using System;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;
using System.Reflection;
using System.IO;

namespace LODMeshThingy {
    public class LODMeshGenerator : EditorWindow {

        static private string LastSaveLocationString;

        #region Public Properties

        public Mesh Source;
        [Range(0.01f, 1)] public float Quality1 = 0.5f;
        [Range(0, 1)] public float Quality2 = 0;

        // generation settings
        [Range(0.1f, 16)] public float Aggressiveness = 7;
        public bool PreserveBorderEdges = false;
        public bool PreserveUVSeamEdges = false;
        public bool PreserveUVFoldoverEdges = false;
        public bool PreserveSurfaceCurvature = true;
        
        // preview
        public Material RenderMaterial;

        #endregion // Public Properties

        [NonSerialized] private UnityEditor.Editor m_SourceMeshEditor;
        [NonSerialized] private UnityEditor.Editor m_LOD1MeshEditor;
        [NonSerialized] private UnityEditor.Editor m_LOD2MeshEditor;

        [NonSerialized] private Mesh m_LOD1;
        [NonSerialized] private Mesh m_LOD2;
        [SerializeField] private Vector2 m_Scroll;
        [SerializeField] private Vector2 m_PreviewDir;

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_SourceProperty;
        private SerializedProperty m_Quality1Property;
        private SerializedProperty m_Quality2Property;
        private SerializedProperty m_AggressivenessProperty;
        private SerializedProperty m_PreserveBorderEdgesProperty;
        private SerializedProperty m_PreserveUVSeamEdgesProperty;
        private SerializedProperty m_PreserveUVFoldoverEdgesProperty;
        private SerializedProperty m_PreserveSurfaceCurvatureProperty;
        private SerializedProperty m_RenderMaterialProperty;

        static private GUIStyle s_LODInfoStyle;
        static private GUIStyle s_LODRenderBoxStyle;
        static private GUIContent s_SharedContent;
        
        static private FieldInfo s_PreviewDirField;
        static private FieldInfo s_MaterialField;
        static private Material s_DefaultMaterial;

        private void OnGUI() {
            SharedInit();

            m_SerializedObject.UpdateIfRequiredOrScript();

            using(new EditorGUILayout.HorizontalScope(s_LODRenderBoxStyle)) {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_SourceProperty, TempContent("Source Mesh"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        DestroyResource(ref m_LOD1);
                        DestroyResource(ref m_LOD2);
                    }
                    EditorGUILayout.PropertyField(m_Quality1Property, TempContent("LOD 1 Quality"));
                    EditorGUILayout.PropertyField(m_Quality2Property, TempContent("LOD 2 Quality"));
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_AggressivenessProperty, TempContent("Aggressiveness"));
                    using(new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.PrefixLabel("Preserve");
                        EditorGUILayout.PropertyField(m_PreserveSurfaceCurvatureProperty, TempContent("Surface Curvature"));
                        EditorGUILayout.PropertyField(m_PreserveBorderEdgesProperty, TempContent("Border Edges"));
                        EditorGUILayout.PropertyField(m_PreserveUVSeamEdgesProperty, TempContent("UV Seam Edges"));
                        EditorGUILayout.PropertyField(m_PreserveUVFoldoverEdgesProperty, TempContent("UV Foldover Edges"));
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_RenderMaterialProperty, TempContent("Preview Material"));
                    
                    m_SerializedObject.ApplyModifiedProperties();
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(300))) {
                    using(new EditorGUI.DisabledScope(Source == null)) {
                        if (GUILayout.Button("Preview")) {
                            GenerateMeshes();
                        }
                    }
                    using(new EditorGUI.DisabledScope(Source == null || (!m_LOD1 && !m_LOD2))) {
                        if (GUILayout.Button("Save")) {

                        }
                    }
                }
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, false, false); {
                using(new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true))) {
                    if (Source != null) {
                        UnityEditor.Editor.CreateCachedEditor(Source, null, ref m_SourceMeshEditor);
                        RenderMeshPreview(m_SourceMeshEditor, Source, 0, RenderMaterial, "LOD 0:\n", ref m_PreviewDir);
                    }
                    if (m_LOD1 != null) {
                        UnityEditor.Editor.CreateCachedEditor(m_LOD1, null, ref m_LOD1MeshEditor);
                        RenderMeshPreview(m_LOD1MeshEditor, Source, 1, RenderMaterial, "LOD 1:\n", ref m_PreviewDir);
                    }
                    if (m_LOD2 != null) {
                        UnityEditor.Editor.CreateCachedEditor(m_LOD2, null, ref m_LOD2MeshEditor);
                        RenderMeshPreview(m_LOD2MeshEditor, Source, 2, RenderMaterial, "LOD 2:\n", ref m_PreviewDir);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            m_SerializedObject.ApplyModifiedProperties();

            if (Source || m_LOD1 || m_LOD2) {
                // Repaint();
            }
        }

        private void GenerateMeshes() {
            MeshSimplifier simplifier = new MeshSimplifier();
            SimplificationOptions options = simplifier.SimplificationOptions;
            options.Agressiveness = Aggressiveness;
            options.PreserveBorderEdges = PreserveBorderEdges;
            options.PreserveUVSeamEdges = PreserveUVSeamEdges;
            options.PreserveUVFoldoverEdges = PreserveUVFoldoverEdges;
            options.PreserveSurfaceCurvature = PreserveSurfaceCurvature;
            simplifier.SimplificationOptions = options;
            try {
                EditorUtility.DisplayProgressBar("Generating LOD Meshes", "LOD 1...", 0f);
                GenerateMesh(simplifier, Source, Quality1, ref m_LOD1);
                EditorUtility.DisplayProgressBar("Generating LOD Meshes", "LOD 2...", 0.5f);
                GenerateMesh(simplifier, Source, Quality2, ref m_LOD2);
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        #region Unity Events

        private void OnEnable() {
            m_SerializedObject = new SerializedObject(this);

            m_SourceProperty = m_SerializedObject.FindProperty("Source");
            m_Quality1Property = m_SerializedObject.FindProperty("Quality1");
            m_Quality2Property = m_SerializedObject.FindProperty("Quality2");
            m_RenderMaterialProperty = m_SerializedObject.FindProperty("RenderMaterial");
            m_AggressivenessProperty = m_SerializedObject.FindProperty("Aggressiveness");
            m_PreserveBorderEdgesProperty = m_SerializedObject.FindProperty("PreserveBorderEdges");
            m_PreserveUVSeamEdgesProperty = m_SerializedObject.FindProperty("PreserveUVSeamEdges");
            m_PreserveUVFoldoverEdgesProperty = m_SerializedObject.FindProperty("PreserveUVFoldoverEdges");
            m_PreserveSurfaceCurvatureProperty = m_SerializedObject.FindProperty("PreserveSurfaceCurvature");

            minSize = new Vector2(1280, 600);
            titleContent = new GUIContent("LOD Mesh Generator");
        }

        private void OnDisable() {
            DestroyResource(ref m_SourceMeshEditor);
            DestroyResource(ref m_LOD1MeshEditor);
            DestroyResource(ref m_LOD2MeshEditor);
        }

        private void OnDestroy() {
            DestroyResource(ref m_LOD1);
            DestroyResource(ref m_LOD2);
        }

        #endregion // Unity Events

        [MenuItem("Aqualab/LOD Generator")]
        static private void Create() {
            var window = EditorWindow.GetWindow<LODMeshGenerator>();
            window.Show();
        }

        #region Operations

        static private void GenerateMesh(MeshSimplifier simplifier, Mesh source, float quality, ref Mesh lod) {
            DestroyResource(ref lod);

            if (quality < 1 && quality > 0) {
                simplifier.Initialize(source);
                simplifier.SimplifyMesh(quality);
                lod = simplifier.ToMesh();
            }
        }

        static private void RenderMeshPreview(UnityEditor.Editor editor, Mesh sourceMesh, int lodLevel, Material renderMaterial, string text, ref Vector2 direction) {
            if (!renderMaterial) {
                renderMaterial = s_DefaultMaterial;
            }

            if (Event.current.type == EventType.Repaint) {
                s_MaterialField.SetValue(editor, renderMaterial);
            }

            Rect r = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandHeight(true));
            GUI.Box(r, "", s_LODRenderBoxStyle);
            r.x += 4;
            r.y += 4;
            r.width -= 8;
            r.height -= 8;
            if (direction == default(Vector2)) {
                direction = (Vector2) s_PreviewDirField.GetValue(editor);
            } else {
                s_PreviewDirField.SetValue(editor, direction);
            }
            Rect editorRect = r;
            editorRect.height -= 24;
            editor.OnInteractivePreviewGUI(editorRect, EditorStyles.helpBox);
            direction = (Vector2) s_PreviewDirField.GetValue(editor);

            if (Event.current.type == EventType.Repaint) {
                Rect labelRect = r;
                labelRect.height = 100;
                GUI.Label(labelRect, text + editor.GetInfoString(), s_LODInfoStyle);
            }

            Rect saveAsButton = default;
            saveAsButton.x = r.xMax - 200;
            saveAsButton.y = r.yMax - 20;
            saveAsButton.width = 200;
            saveAsButton.height = 20;

            Mesh mesh = editor.target as Mesh;

            if (mesh != sourceMesh) {
                if (GUI.Button(saveAsButton, "Save As")) {
                    string sourceName = sourceMesh.name + "_LOD" + lodLevel.ToString();
                    SaveResourceAs(mesh, sourceName);
                }
            }
        }

        static private void SaveResourceAs(Mesh mesh, string name) {
            string lastDirectory = EditorPrefs.GetString(LastSaveLocationString, "Assets/");
            string path = EditorUtility.SaveFilePanelInProject("Save LOD Mesh", name, "mesh", "Save this mesh", lastDirectory);
            if (!string.IsNullOrEmpty(path)) {
                Mesh clone = Instantiate(mesh);
                clone.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(clone, path);
                lastDirectory = Path.GetDirectoryName(path);
                EditorPrefs.SetString(LastSaveLocationString, lastDirectory);
            }
        }

        static private void SaveResource(Mesh mesh, string path) {

        }

        static private void DestroyResource<T>(ref T obj) where T : UnityEngine.Object {
            if (obj != null) {
                DestroyImmediate(obj);
                obj = null;
            }
        }

        #endregion // Operations

        #region Shared

        static private GUIContent TempContent(string label) {
            if (s_SharedContent == null) {
                s_SharedContent = new GUIContent(label);
            } else {
                s_SharedContent.text = label;
            }
            return s_SharedContent;
        }

        static private void SharedInit() {
            if (s_PreviewDirField == null) {
                Type modelInspectorType = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.ModelInspector");
                s_PreviewDirField = modelInspectorType?.GetField("previewDir"); 
                s_MaterialField = modelInspectorType?.GetField("m_Material", BindingFlags.Instance | BindingFlags.NonPublic);

                s_DefaultMaterial = (Material) typeof(Material).GetMethod("GetDefaultMaterial", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }

            if (s_LODInfoStyle == null) {
                s_LODInfoStyle = new GUIStyle(EditorStyles.label);
                s_LODInfoStyle.fontStyle = FontStyle.Bold;
                s_LODInfoStyle.alignment = TextAnchor.UpperLeft;
                s_LODInfoStyle.margin = new RectOffset(4, 4, 4, 4);

                s_LODRenderBoxStyle = new GUIStyle(EditorStyles.helpBox);
            }

            if (s_SharedContent == null) {
                s_SharedContent = new GUIContent();
            }

            if (LastSaveLocationString == null) {
                LastSaveLocationString = PlayerSettings.productGUID + "/LastLODSaveDirectory";
            }
        }

        #endregion // Init
    }
}