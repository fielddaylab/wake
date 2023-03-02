/*
 * Copyright (C) 2022. Autumn Beauchesne, Field Day Lab
 * Author:  Autumn Beauchesne
 * Date:    4 Feb 2022
 * 
 * File:    StreamingUGUITexture.cs
 * Purpose: Streaming texture, rendered in UGUI with a RawTexture.
 */

#if UNITY_2018_3_OR_NEWER
#define USE_ALWAYS
#endif // UNITY_2018_3_OR_NEWER

#if USING_BEAUUTIL
using BeauUtil;
#endif // USING_BEAUUTIL

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace EasyAssetStreaming {
    #if USE_ALWAYS
    [ExecuteAlways]
    #else
    [ExecuteInEditMode]
    #endif // USE_ALWAYS
    [AddComponentMenu("Streaming Assets/Streaming UGUI Texture")]
    [RequireComponent(typeof(RawImage), typeof(RectTransform))]
    public sealed class StreamingUGUITexture : UIBehaviour, IStreamingTextureComponent, ILayoutSelfController {
        #region Inspector

        [SerializeField] private RawImage m_RawImage;
        #if USING_BEAUUTIL
        [SerializeField] private ColorGroup m_ColorGroup;
        #endif // USING_BEAUUTIL

        [SerializeField, StreamingImagePath, FormerlySerializedAs("m_Url")] private string m_Path;
        [SerializeField] private Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField] private AutoSizeMode m_AutoSize = AutoSizeMode.Disabled;
        [SerializeField] private bool m_Visible = true;

        #endregion // Inspector

        [NonSerialized] private StreamingHelper.AwakeTracker m_Awake;
        [NonSerialized] private StreamingAssetHandle m_AssetHandle;
        [NonSerialized] private Texture m_LoadedTexture;
        [NonSerialized] private Rect m_ClippedUVs;
        [NonSerialized] private Vector2 m_AppliedPivot;
        [NonSerialized] private bool m_ResizeGuard;
        private readonly Streaming.AssetCallback m_OnUpdatedEvent;
        private DrivenRectTransformTracker m_Tracker;

        private StreamingUGUITexture() {
            m_OnUpdatedEvent = (StreamingAssetHandle id, Streaming.AssetStatus status, object asset) => {
                if (status == Streaming.AssetStatus.Loaded) {
                    m_LoadedTexture = (Texture) asset;
                    m_RawImage.texture = m_LoadedTexture;
                    ApplyVisible();
                    Resize(m_AutoSize);
                } else {
                    m_LoadedTexture = null;
                    m_RawImage.texture = null;
                    ApplyVisible();
                }
                OnUpdated?.Invoke(this, status);
            };
        }

        /// <summary>
        /// Event invoked when asset status is updated.
        /// </summary>
        public event StreamingComponentEvent OnUpdated;

        #region Properties

        /// <summary>
        /// Path or URL to the texture.
        /// </summary>
        public string Path {
            get { return m_Path; }
            set {
                if (m_Path != value) {
                    m_Path = value;
                    if (isActiveAndEnabled) {
                        LoadTexture();
                    }
                }
            }
        }

        /// <summary>
        /// Returns if the texture is fully loaded.
        /// </summary>
        public bool IsLoaded() {
            return Streaming.IsLoaded(m_AssetHandle);
        }

        /// <summary>
        /// Returns if the texture is currently loading.
        /// </summary>
        public bool IsLoading() {
            return (Streaming.Status(m_AssetHandle) & Streaming.AssetStatus.PendingLoad) != 0;;
        }

        /// <summary>
        /// Loaded texture.
        /// </summary>
        public Texture Texture {
            get { return m_LoadedTexture; }
        }

        /// <summary>
        /// Color of the renderer.
        /// </summary>
        public Color Color {
            get {
                #if USING_BEAUUTIL
                if (m_ColorGroup) {
                    return m_ColorGroup.Color;
                }
                #endif // USING_BEAUUTIL
                return m_RawImage.color;
            }
            set {
                #if USING_BEAUUTIL
                if (m_ColorGroup) {
                    m_ColorGroup.Color = value;
                    return;
                }
                #endif // USING_BEAUUTIL
                m_RawImage.color = value;
            }
        }

        /// <summary>
        /// Transparency of the renderer.
        /// </summary>
        public float Alpha {
            get {
                #if USING_BEAUUTIL
                if (m_ColorGroup) {
                    return m_ColorGroup.GetAlpha();
                }
                #endif // USING_BEAUUTIL
                return m_RawImage.color.a;
            }
            set {
                #if USING_BEAUUTIL
                if (m_ColorGroup) {
                    m_ColorGroup.SetAlpha(value);
                    return;
                }
                #endif // USING_BEAUUTIL
                
                var rawColor = m_RawImage.color;
                if (rawColor.a != value) {
                    rawColor.a = value;
                    m_RawImage.color = rawColor;
                }
            }
        }

        /// <summary>
        /// Whether or not the renderer is visible.
        /// </summary>
        public bool Visible {
            get { return m_Visible; }
            set {
                if (m_Visible != value) {
                    m_Visible = value;
                    if (isActiveAndEnabled) {
                        ApplyVisible();
                    }
                }
            }
        }

        /// <summary>
        /// UV window.
        /// </summary>
        public Rect UVRect {
            get { return m_UVRect; }
            set {
                if (m_UVRect != value) {
                    m_UVRect = value;
                    Resize(m_AutoSize);
                }
            }
        }

        /// <summary>
        /// Auto sizing mode.
        /// </summary>
        public AutoSizeMode SizeMode {
            get { return m_AutoSize; }
            set {
                if (m_AutoSize != value) {
                    m_AutoSize = value;
                    Resize(m_AutoSize);
                }
            }
        }

        /// <summary>
        /// Size in local space.
        /// </summary>
        public Vector2 Size {
            get { return ((RectTransform) transform).rect.size; }
            set {
                RectTransform rect = (RectTransform) transform;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
            }
        }

        #endregion // Properties

        /// <summary>
        /// Resizes the mesh to preserve aspect ratio.
        /// </summary>
        public void Resize(AutoSizeMode sizeMode) {
            if (m_ResizeGuard) {
                return;
            }

            if (sizeMode == AutoSizeMode.Disabled || !m_LoadedTexture) {
                m_Tracker.Clear();
                if (m_ClippedUVs != m_UVRect) {
                    m_ClippedUVs = m_UVRect;
                    LoadClipping();
                }
                return;
            }

            RectTransform rect = (RectTransform) transform;
            Vector2 size = rect.rect.size;

            m_Tracker.Clear();

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDeltaX);
                    break;
                }
                case AutoSizeMode.StretchY: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDeltaY);
                    break;
                }
                case AutoSizeMode.FitToParent:
                case AutoSizeMode.FillParent:
                case AutoSizeMode.FillParentWithClipping: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDelta | DrivenTransformProperties.Anchors);
                    break;
                }
            }

            StreamingHelper.UpdatedResizeProperty updated = StreamingHelper.AutoSize(sizeMode, m_LoadedTexture, m_UVRect, rect.localPosition, rect.pivot, ref size, ref m_ClippedUVs, ref m_AppliedPivot, StreamingHelper.GetParentSize(rect));
            if (updated == 0) {
                return;
            }

            m_ResizeGuard = true;

            if ((updated & StreamingHelper.UpdatedResizeProperty.Pivot) != 0) {
                LoadAnchors();
            }

            if ((updated & StreamingHelper.UpdatedResizeProperty.Size) != 0) {
                switch(sizeMode) {
                    case AutoSizeMode.StretchX: {
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                        break;
                    }
                    case AutoSizeMode.StretchY: {
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                        break;
                    }
                    case AutoSizeMode.FitToParent:
                    case AutoSizeMode.FillParent:
                    case AutoSizeMode.FillParentWithClipping: {
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                        break;
                    }
                }
            }

            if ((updated & StreamingHelper.UpdatedResizeProperty.Clip) != 0) {
                LoadClipping();
            }

            m_ResizeGuard = false;
        }

        #region Unity Events

        protected override void Awake() {
            m_Awake.OnNaturalAwake();
        }

        protected override void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) {
                    return;
                }
                
                m_RawImage = GetComponent<RawImage>();
                #if USING_BEAUUTIL
                m_ColorGroup = GetComponent<ColorGroup>();
                #endif // USING_BEAUUTIL

                if (m_ClippedUVs == default) {
                    m_ClippedUVs = m_UVRect;
                }
                if (m_AppliedPivot == default) {
                    m_AppliedPivot = m_RawImage.rectTransform.pivot;
                }

                LoadTexture();
                LoadClipping();
                LoadAnchors();
                ApplyVisible();
                return;
            }
            #endif // UNITY_EDITOR

            if (m_ClippedUVs == default) {
                m_ClippedUVs = m_UVRect;
            }
            if (m_AppliedPivot == default) {
                m_AppliedPivot = m_RawImage.rectTransform.pivot;
            }

            if (!m_Awake.IsForcing()) {
                LoadTexture();
                LoadClipping();
                LoadAnchors();
                ApplyVisible();
            }
        }

        protected override void OnDisable() {
            m_Tracker.Clear();
            Unload();
        }

        protected override void OnDestroy() {
            Unload();
        }

        protected override void OnRectTransformDimensionsChange() {
            Resize(m_AutoSize);
        }

        protected override void OnDidApplyAnimationProperties() {
            if (isActiveAndEnabled) {
                LoadClipping();
                LoadAnchors();
                ApplyVisible();
            }
        } 

        #if UNITY_EDITOR

        [NonSerialized] private Vector2 m_CachedParentSize;

        private void Update() {
            if (!Application.IsPlaying(this)) {
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    return;
                }

                Vector2? parentSize = StreamingHelper.GetParentSize(transform);
                if (parentSize.HasValue && m_CachedParentSize != parentSize.Value) {
                    m_CachedParentSize = parentSize.Value;
                    Resize(m_AutoSize);
                }
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Resources

        /// <summary>
        /// Prefetches
        /// </summary>
        public void Preload() {
            m_Awake.AwakeIfNotAwoken(this);
            LoadTexture();
            LoadClipping();
            LoadAnchors();
            ApplyVisible();
        }

        private void LoadTexture() {
            if (!Streaming.Texture(m_Path, ref m_AssetHandle, m_OnUpdatedEvent)) {
                if (!m_AssetHandle) {
                    m_RawImage.enabled = false;
                    #if USING_BEAUUTIL
                    if (m_ColorGroup) {
                        m_ColorGroup.Visible = false;
                    }
                    #endif // USING_BEAUUTIL
                    m_LoadedTexture = null;
                }
                return;
            }

            bool bHasTexture = m_LoadedTexture;

            m_RawImage.enabled = bHasTexture && m_Visible;
            m_RawImage.texture = m_LoadedTexture;
            m_ClippedUVs = m_UVRect;

            #if USING_BEAUUTIL
            if (m_ColorGroup) {
                m_ColorGroup.Visible = m_RawImage.enabled;
            }
            #endif // USING_BEAUUTIL

            Streaming.AssetStatus status = Streaming.Status(m_AssetHandle);
            if ((status & Streaming.AssetStatus.Loaded) != 0) {
                Resize(m_AutoSize);
            } else {
                OnUpdated?.Invoke(this, status);
            }
        }

        private void LoadClipping() {
            m_RawImage.uvRect = m_ClippedUVs;
        }

        private void LoadAnchors() {
            if (StreamingHelper.ControlsAnchors(m_AutoSize)) {
                RectTransform rect = m_RawImage.rectTransform;
                rect.anchorMin = rect.anchorMax = m_AppliedPivot;
            }
        }

        private void ApplyVisible() {
            m_RawImage.enabled = m_LoadedTexture && m_Visible;
            #if USING_BEAUUTIL
            if (m_ColorGroup) {
                m_ColorGroup.Visible = m_RawImage.enabled;
            }
            #endif // USING_BEAUUTIL
        }

        /// <summary>
        /// Unloads all resources owned by the StreamingWorldTexture.
        /// </summary>
        public void Unload() {
            if (m_RawImage) {
                m_RawImage.enabled = false;
                m_RawImage.texture = null;
            }

            #if USING_BEAUUTIL
            if (m_ColorGroup) {
                m_ColorGroup.Visible = false;
            }
            #endif // USING_BEAUUTIL

            if (Streaming.Unload(ref m_AssetHandle, m_OnUpdatedEvent)) {
                m_LoadedTexture = null;
                OnUpdated?.Invoke(this, Streaming.AssetStatus.Unloaded);
            }
        }

        #endregion // Resources

        #region ILayoutSelfController

        void ILayoutController.SetLayoutHorizontal() {
            if (StreamingHelper.IsAutoSizeHorizontal(m_AutoSize)) {
                Resize(m_AutoSize);
            }
        }

        void ILayoutController.SetLayoutVertical() {
            if (StreamingHelper.IsAutoSizeVertical(m_AutoSize)) {
                Resize(m_AutoSize);
            }
        }

        #endregion // ILayoutSelfController

        #region Editor

        #if UNITY_EDITOR

        protected override void Reset() {
            m_RawImage = GetComponent<RawImage>();
            #if USING_BEAUUTIL
            m_ColorGroup = GetComponent<ColorGroup>();
            #endif // USING_BEAUUTIL
        }

        protected override void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this)) {
                return;
            }

            m_RawImage = GetComponent<RawImage>();
            #if USING_BEAUUTIL
            m_ColorGroup = GetComponent<ColorGroup>();
            #endif // USING_BEAUUTIL

            if (this.isActiveAndEnabled) {
                EditorApplication.delayCall += () => {
                    if (!this || !this.isActiveAndEnabled) {
                        return;
                    }

                    LoadTexture();
                    Resize(m_AutoSize);
                    LoadClipping();
                    LoadAnchors();
                    ApplyVisible();
                };
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}