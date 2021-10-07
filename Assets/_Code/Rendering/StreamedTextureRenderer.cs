using System;
using UnityEngine;
using BeauUtil;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {

    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class StreamedTextureRenderer : MonoBehaviour, IScenePreloader, ISceneUnloadHandler {

        #region Inspector

        [SerializeField] private MeshFilter m_MeshFilter;
        [SerializeField] private MeshRenderer m_MeshRenderer;

        [SerializeField, StreamingPath("png,jpg,jpeg")] private string m_Url;
        [SerializeField, Required] private Material m_Material = null;
        [SerializeField] private Vector2 m_Size = new Vector2(1, 1);
        [SerializeField] private Vector2 m_Pivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private Color32 m_Color = Color.white;
        [SerializeField, SortingLayer] private int m_SortingLayer = 0;
        [SerializeField] private int m_SortingOrder = 0;

        #endregion // Inspector

        [NonSerialized] private Mesh m_MeshInstance;
        [NonSerialized] private Texture2D m_LoadedTexture;
        [NonSerialized] private Material m_MaterialInstance;

        #if UNITY_EDITOR

        private void OnEnable() {
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            RebuildMesh();
            RebuildMaterial();
        }

        #endif // UNITY_EDITOR

        private void OnDestroy() {
            UnloadResources();
        }

        private void RebuildMesh() {
            m_MeshInstance = RenderUtils.CreateQuad(m_Size, m_Pivot, m_Color, m_MeshInstance);
            m_MeshInstance.hideFlags = HideFlags.DontSave;
            m_MeshFilter.sharedMesh = m_MeshInstance;
        }

        private void RebuildMaterial() {
            if (!m_Material) {
                return;
            }

            if (!m_MaterialInstance) {
                m_MaterialInstance = new Material(m_Material);
                m_MaterialInstance.hideFlags = HideFlags.DontSave;
                m_MeshRenderer.sharedMaterial = m_MaterialInstance;
            } else {
                m_MaterialInstance.shader = m_Material.shader;
                m_MaterialInstance.CopyPropertiesFromMaterial(m_Material);
            }

            Streaming.Texture(m_Url, ref m_LoadedTexture);
            m_MaterialInstance.mainTexture = m_LoadedTexture;

            m_MeshRenderer.sortingLayerID = m_SortingLayer;
            m_MeshRenderer.sortingOrder = m_SortingOrder;
        }

        private void UnloadResources() {
            if (m_MeshRenderer) {
                m_MeshRenderer.enabled = false;
            }

            Streaming.Unload(ref m_LoadedTexture);

            if (m_MaterialInstance != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    Material.DestroyImmediate(m_MaterialInstance);
                } else {
                    Material.Destroy(m_MaterialInstance);
                }
                #else
                Material.Destroy(m_MaterialInstance);
                #endif // UNITY_EDITOR

                m_MaterialInstance = null;
            }

            if (m_MeshInstance != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    Mesh.DestroyImmediate(m_MeshInstance);
                } else {
                    Mesh.Destroy(m_MeshInstance);
                }
                #else
                Mesh.Destroy(m_MeshInstance);
                #endif // UNITY_EDITOR

                m_MeshInstance = null;
            }
        }

        #region Scene Loading

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext) {
            RebuildMesh();
            RebuildMaterial();
            return null;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext) {
            UnloadResources();
        }

        #endregion // Scene Loading

        #if UNITY_EDITOR

        private void Reset() {
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this))
                return;

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            EditorApplication.delayCall += () => {
                if (!this) {
                    return;
                }
                RebuildMesh();
                RebuildMaterial();
            };
        }

        private void FixAspectRatioY() {
            float aspectRatio = (float) m_LoadedTexture.height / m_LoadedTexture.width;
            m_Size.y = m_Size.x * aspectRatio;
        }

        private void FixAspectRatioX() {
            float aspectRatio = (float) m_LoadedTexture.width / m_LoadedTexture.height;
            m_Size.x = m_Size.y * aspectRatio;
        }

        [CustomEditor(typeof(StreamedTextureRenderer)), CanEditMultipleObjects]
        private class Inspector : Editor {
            private SerializedProperty m_UrlProperty;
            private SerializedProperty m_MaterialProperty;
            private SerializedProperty m_SizeProperty;
            private SerializedProperty m_PivotProperty;
            private SerializedProperty m_ColorProperty;
            private SerializedProperty m_SortingLayerProperty;
            private SerializedProperty m_SortingOrderProperty;

            private void OnEnable() {
                m_UrlProperty = serializedObject.FindProperty("m_Url");
                m_MaterialProperty = serializedObject.FindProperty("m_Material");
                m_SizeProperty = serializedObject.FindProperty("m_Size");
                m_PivotProperty = serializedObject.FindProperty("m_Pivot");
                m_ColorProperty = serializedObject.FindProperty("m_Color");
                m_SortingLayerProperty = serializedObject.FindProperty("m_SortingLayer");
                m_SortingOrderProperty = serializedObject.FindProperty("m_SortingOrder");
            }

            public override void OnInspectorGUI() {
                serializedObject.UpdateIfRequiredOrScript();

                bool bRendererUpdated = false;

                EditorGUI.BeginChangeCheck(); {
                    EditorGUILayout.PropertyField(m_UrlProperty);
                    EditorGUILayout.PropertyField(m_MaterialProperty);
                }
                if (EditorGUI.EndChangeCheck()) {
                    bRendererUpdated = true;
                }

                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck(); {
                    EditorGUILayout.PropertyField(m_SortingLayerProperty);
                    EditorGUILayout.PropertyField(m_SortingOrderProperty);
                }
                if (EditorGUI.EndChangeCheck()) {
                    bRendererUpdated = true;
                }

                if (bRendererUpdated) {
                    serializedObject.ApplyModifiedProperties();
                    foreach(StreamedTextureRenderer renderer in targets) {
                        renderer.RebuildMaterial();
                    }
                }

                EditorGUILayout.Space();

                bool bMeshUpdated = false;
                
                EditorGUI.BeginChangeCheck(); {
                    EditorGUILayout.PropertyField(m_SizeProperty);
                    EditorGUILayout.PropertyField(m_PivotProperty);
                    EditorGUILayout.PropertyField(m_ColorProperty);
                }
                if (EditorGUI.EndChangeCheck()) {
                    bMeshUpdated = true;
                }

                EditorGUILayout.Space();

                GUI.enabled = !m_UrlProperty.hasMultipleDifferentValues && !string.IsNullOrEmpty(m_UrlProperty.stringValue)
                    && !m_MaterialProperty.hasMultipleDifferentValues && m_MaterialProperty.objectReferenceValue != null
                    && !Application.isPlaying;
                
                EditorGUILayout.BeginVertical(); {
                    if (GUILayout.Button("Resize X for Aspect Ratio")) {
                        serializedObject.ApplyModifiedProperties();
                        foreach(StreamedTextureRenderer renderer in targets) {
                            renderer.FixAspectRatioX();
                        }
                        bMeshUpdated = true;
                    }
                    if (GUILayout.Button("Resize Y for Aspect Ratio")) {
                        serializedObject.ApplyModifiedProperties();
                        foreach(StreamedTextureRenderer renderer in targets) {
                            renderer.FixAspectRatioY();
                        }
                        bMeshUpdated = true;
                    }
                }
                EditorGUILayout.EndVertical();
                
                GUI.enabled = true;

                if (bMeshUpdated) {
                    foreach(StreamedTextureRenderer renderer in targets) {
                        renderer.RebuildMesh();
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR
    }
}