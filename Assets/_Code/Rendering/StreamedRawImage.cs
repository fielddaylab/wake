using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {

    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class StreamedRawImage : MonoBehaviour, IScenePreloader, ISceneUnloadHandler {

        #region Inspector

        [SerializeField] private RawImage m_RawImage;
        [SerializeField, StreamingPath("png,jpg,jpeg")] private string m_Url;

        #endregion // Inspector

        [NonSerialized] private Texture2D m_LoadedTexture;

        #if UNITY_EDITOR

        private void OnEnable() {
            m_RawImage = GetComponent<RawImage>();
            RefreshTexture();
        }

        #endif // UNITY_EDITOR

        private void OnDestroy() {
            UnloadResources();
        }

        private void RefreshTexture() {
            Streaming.Texture(m_Url, ref m_LoadedTexture);
            m_RawImage.texture = m_LoadedTexture;
        }

        private void UnloadResources() {
            if (m_RawImage) {
                m_RawImage.enabled = false;
            }

            Streaming.Unload(ref m_LoadedTexture);
        }

        #region Scene Loading

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext) {
            RefreshTexture();
            return null;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext) {
            UnloadResources();
        }

        #endregion // Scene Loading

        #if UNITY_EDITOR

        private void Reset() {
            m_RawImage = GetComponent<RawImage>();
        }

        private void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this))
                return;

            m_RawImage = GetComponent<RawImage>();
            
            EditorApplication.delayCall += () => {
                if (!this) {
                    return;
                }
                RefreshTexture();
            };
        }

        private void FixAspectRatioY() {
            float aspectRatio = (float) m_LoadedTexture.height / m_LoadedTexture.width;
            RectTransform transform = m_RawImage.rectTransform;
            Undo.RecordObject(transform, "Resizing RectTransform");
            Vector2 size = transform.sizeDelta;
            size.y = size.x * aspectRatio;
            transform.sizeDelta = size;
            EditorUtility.SetDirty(transform);
        }

        private void FixAspectRatioX() {
            float aspectRatio = (float) m_LoadedTexture.width / m_LoadedTexture.height;
            RectTransform transform = m_RawImage.rectTransform;
            Undo.RecordObject(transform, "Resizing RectTransform");
            Vector2 size = transform.sizeDelta;
            size.x = size.y * aspectRatio;
            transform.sizeDelta = size;
            EditorUtility.SetDirty(transform);
        }

        [CustomEditor(typeof(StreamedRawImage)), CanEditMultipleObjects]
        private class Inspector : Editor {
            private SerializedProperty m_UrlProperty;

            private void OnEnable() {
                m_UrlProperty = serializedObject.FindProperty("m_Url");
            }

            public override void OnInspectorGUI() {
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUI.BeginChangeCheck(); {
                    EditorGUILayout.PropertyField(m_UrlProperty);
                }
                if (EditorGUI.EndChangeCheck()) {
                    serializedObject.ApplyModifiedProperties();
                    foreach(StreamedRawImage renderer in targets) {
                        renderer.RefreshTexture();
                    }
                }

                EditorGUILayout.Space();

                GUI.enabled = !m_UrlProperty.hasMultipleDifferentValues && !string.IsNullOrEmpty(m_UrlProperty.stringValue)
                    && !Application.isPlaying;
                
                EditorGUILayout.BeginVertical(); {
                    if (GUILayout.Button("Resize X for Aspect Ratio")) {
                        serializedObject.ApplyModifiedProperties();
                        foreach(StreamedRawImage renderer in targets) {
                            renderer.FixAspectRatioX();
                        }
                    }

                    if (GUILayout.Button("Resize Y for Aspect Ratio")) {
                        serializedObject.ApplyModifiedProperties();
                        foreach(StreamedRawImage renderer in targets) {
                            renderer.FixAspectRatioY();
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                
                GUI.enabled = true;

                serializedObject.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR
    }
}