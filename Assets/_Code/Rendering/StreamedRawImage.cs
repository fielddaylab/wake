using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System.Collections;
using BeauRoutine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {

    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class StreamedRawImage : MonoBehaviour, IScenePreloader, ISceneUnloadHandler {

        public enum AutoSizeMode {
            Disabled,
            StretchX,
            StretchY
        }

        #region Inspector

        [SerializeField] private RawImage m_RawImage;
        [SerializeField, StreamingPath("png,jpg,jpeg")] private string m_Url;
        [SerializeField, AutoEnum] private AutoSizeMode m_AutoSizeMode = AutoSizeMode.Disabled;
        [SerializeField] private GameObject m_LoadingIndicator = null;

        #endregion // Inspector

        [NonSerialized] private Texture2D m_LoadedTexture;
        [NonSerialized] private Routine m_LoadingRoutine;

        public string URL {
            get { return m_Url; }
            set {
                if (m_Url != value) {
                    m_Url = value;
                    if (isActiveAndEnabled) {
                        RefreshTexture();
                    }
                }
            }
        }

        public void Prefetch() {
            RefreshTexture();
        }

        public bool IsLoaded() {
            return Streaming.IsLoaded(m_LoadedTexture);
        }

        private void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                m_RawImage = GetComponent<RawImage>();
                RefreshTexture();
                return;
            }
            #endif // UNITY_EDITOR

            if (Services.State.IsLoadingScene()) {
                return;
            }
            
            RefreshTexture();
        }

        private void OnDisable() {
            UnloadResources();
        }

        private void OnDestroy() {
            UnloadResources();
        }

        private void RefreshTexture() {
            if (!Streaming.Texture(m_Url, ref m_LoadedTexture)) {
                return;
            }
            
            m_RawImage.texture = m_LoadedTexture;

            #if UNITY_EDITOR
            if (!EditorApplication.isPlaying) {
                m_RawImage.enabled = m_LoadedTexture;
                if (m_LoadedTexture)
                    AdjustAspectRatio();
                return;
            }
            #endif // UNITY_EDITOR

            if (m_LoadedTexture) {
                if (Streaming.IsLoaded(m_Url)) {
                    m_LoadingRoutine.Stop();
                    UpdateLoadingIndicator(false, true);
                    AdjustAspectRatio();
                } else {
                    UpdateLoadingIndicator(true, false);
                    m_LoadingRoutine.Replace(this, LoadWait());
                }
            } else {
                m_LoadingRoutine.Stop();
                UpdateLoadingIndicator(false, false);
            }
        }

        private void UpdateLoadingIndicator(bool inbIndicator, bool inbShowImage) {
            if (m_LoadingIndicator != null) {
                m_LoadingIndicator.SetActive(inbIndicator);
            }
            m_RawImage.enabled = inbShowImage && !inbIndicator;
        }

        private void UnloadResources() {
            if (m_RawImage) {
                m_RawImage.enabled = false;
            }

            Streaming.Unload(ref m_LoadedTexture);
        }

        private IEnumerator LoadWait() {
            while((Streaming.Status(m_LoadedTexture) & Streaming.AssetStatus.PendingLoad) != 0) {
                yield return null;
            }

            UpdateLoadingIndicator(false, true);
            AdjustAspectRatio();
        }

        private void AdjustAspectRatio() {
            if (m_AutoSizeMode == AutoSizeMode.Disabled) {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                switch(m_AutoSizeMode) {
                    case AutoSizeMode.StretchX: {
                        FixAspectRatioX();
                        break;
                    }

                    case AutoSizeMode.StretchY: {
                        FixAspectRatioY();
                        break;
                    }
                }

                return;
            }
            #endif // UNITY_EDITOR

            float aspectRatio;;
            RectTransform transform = m_RawImage.rectTransform;
            Vector2 size = transform.sizeDelta;

            switch(m_AutoSizeMode) {
                case AutoSizeMode.StretchX: {
                    aspectRatio = (float) m_LoadedTexture.width / m_LoadedTexture.height;
                    size.x = size.y * aspectRatio;
                    break;
                }

                case AutoSizeMode.StretchY: {
                    aspectRatio = (float) m_LoadedTexture.height / m_LoadedTexture.width;
                    size.y = size.x * aspectRatio;
                    break;
                }
            }

            transform.sizeDelta = size;
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
            private SerializedProperty m_AutoSizeModeProperty;
            private SerializedProperty m_LoadingIndicatorProperty;

            private void OnEnable() {
                m_UrlProperty = serializedObject.FindProperty("m_Url");
                m_AutoSizeModeProperty = serializedObject.FindProperty("m_AutoSizeMode");
                m_LoadingIndicatorProperty = serializedObject.FindProperty("m_LoadingIndicator");
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

                EditorGUI.BeginChangeCheck(); {
                    EditorGUILayout.PropertyField(m_AutoSizeModeProperty);
                }
                if (EditorGUI.EndChangeCheck()) {
                    serializedObject.ApplyModifiedProperties();
                    foreach(StreamedRawImage renderer in targets) {
                        renderer.AdjustAspectRatio();
                    }
                }

                EditorGUILayout.PropertyField(m_LoadingIndicatorProperty);
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR
    }
}