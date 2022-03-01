using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System.Collections;
using BeauRoutine;
using UnityEngine.Video;
using EasyAssetStreaming;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {

    [ExecuteAlways]
    [RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
    public class StreamedVideoImage : MonoBehaviour, IScenePreloader, ISceneUnloadHandler {

        public enum AutoSizeMode {
            StretchX,
            StretchY
        }

        #region Inspector

        [SerializeField] private RawImage m_RawImage;
        [SerializeField] private VideoPlayer m_VideoPlayer;
        [SerializeField, StreamingVideoPath] private string m_Url;
        [SerializeField, AutoEnum] private AutoSizeMode m_AutoSizeMode = AutoSizeMode.StretchX;
        [SerializeField] private GameObject m_LoadingIndicator = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_LoadingRoutine;

        public string URL {
            get { return m_Url; }
            set {
                if (m_Url != value) {
                    m_Url = value;
                    if (isActiveAndEnabled) {
                        RefreshPlayer();
                    }
                }
            }
        }

        public void Prefetch() {
            RefreshPlayer();
        }

        public bool IsLoaded() {
            return m_VideoPlayer.isPrepared;
        }

        private void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                m_RawImage = GetComponent<RawImage>();
                m_VideoPlayer = GetComponent<VideoPlayer>();
                return;
            }
            #endif // UNITY_EDITOR

            if (Script.IsLoading) {
                return;
            }
            
            RefreshPlayer();
        }

        private void OnDisable() {
            UnloadResources();
        }

        private void OnDestroy() {
            UnloadResources();
        }

        private void RefreshPlayer() {
            if (m_VideoPlayer.url == m_Url) {
                return;
            }

            #if UNITY_EDITOR
            if (!EditorApplication.isPlaying) {
                m_RawImage.texture = null;
                m_RawImage.enabled = false;
                m_VideoPlayer.enabled = false;
                return;
            }
            #endif // UNITY_EDITOR

            if (!string.IsNullOrEmpty(m_Url)) {
                m_VideoPlayer.enabled = true;
                m_VideoPlayer.url = Streaming.ResolvePathToURL(m_Url);
                m_VideoPlayer.Prepare();
                if (m_VideoPlayer.isPrepared) {
                    m_LoadingRoutine.Stop();
                    UpdateLoadingIndicator(false, true);
                    AdjustAspectRatio();
                    m_VideoPlayer.Play();
                } else {
                    UpdateLoadingIndicator(true, false);
                    m_LoadingRoutine.Replace(this, LoadWait());
                }
            } else {
                m_VideoPlayer.Stop();
                m_VideoPlayer.enabled = false;
                m_RawImage.texture = null;
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

            m_VideoPlayer.Stop();
            m_VideoPlayer.enabled = false;
        }

        private IEnumerator LoadWait() {
            while(!m_VideoPlayer.isPrepared) {
                yield return null;
            }

            UpdateLoadingIndicator(false, true);
            AdjustAspectRatio();

            m_RawImage.texture = m_VideoPlayer.texture;
            m_VideoPlayer.Play();
        }

        private void AdjustAspectRatio() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                return;
            }
            #endif // UNITY_EDITOR

            switch(m_AutoSizeMode) {
                case AutoSizeMode.StretchX: {
                    m_RawImage.rectTransform.ResizeXForAspectRatio(m_VideoPlayer.width, m_VideoPlayer.height);
                    break;
                }

                case AutoSizeMode.StretchY: {
                    m_RawImage.rectTransform.ResizeYForAspectRatio(m_VideoPlayer.width, m_VideoPlayer.height);
                    break;
                }
            }
        }

        #region Scene Loading

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext) {
            RefreshPlayer();
            return null;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext) {
            UnloadResources();
        }

        #endregion // Scene Loading

        #if UNITY_EDITOR

        private void Reset() {
            m_RawImage = GetComponent<RawImage>();
            m_VideoPlayer = GetComponent<VideoPlayer>();
            m_VideoPlayer.renderMode = VideoRenderMode.APIOnly;
            m_VideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            m_VideoPlayer.isLooping = true;
            m_VideoPlayer.playOnAwake = false;
        }

        private void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this))
                return;

            m_RawImage = GetComponent<RawImage>();
            m_VideoPlayer = GetComponent<VideoPlayer>();
        }

        [CustomEditor(typeof(StreamedVideoImage)), CanEditMultipleObjects]
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
                    foreach(StreamedVideoImage renderer in targets) {
                        renderer.RefreshPlayer();
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_AutoSizeModeProperty);
                EditorGUILayout.PropertyField(m_LoadingIndicatorProperty);
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR
    }
}