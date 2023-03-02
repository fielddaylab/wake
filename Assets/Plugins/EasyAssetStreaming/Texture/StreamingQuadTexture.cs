/*
 * Copyright (C) 2022. Autumn Beauchesne, Field Day Lab
 * Author:  Autumn Beauchesne
 * Date:    4 Feb 2022
 * 
 * File:    StreamingQuadTexture.cs
 * Purpose: Streaming texture, rendered with a MeshRenderer.
 */

#if UNITY_2018_3_OR_NEWER
#define USE_ALWAYS
#endif // UNITY_2018_3_OR_NEWER

#if USING_BEAUUTIL
using BeauUtil;
#endif // USING_BEAUUTIL

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace EasyAssetStreaming {

    #if USE_ALWAYS
    [ExecuteAlways]
    #else
    [ExecuteInEditMode]
    #endif // USE_ALWAYS
    [AddComponentMenu("Streaming Assets/Streaming Quad Texture")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StreamingQuadTexture : MonoBehaviour, IStreamingTextureComponent, ILayoutSelfController {
        
        #region Inspector

        [SerializeField] private MeshFilter m_MeshFilter;
        [SerializeField] private MeshRenderer m_MeshRenderer;
        #if USING_BEAUUTIL
        [SerializeField] private ColorGroup m_ColorGroup;
        #endif // USING_BEAUUTIL

        [SerializeField, StreamingImagePath, FormerlySerializedAs("m_Url")] private string m_Path = null;
        [SerializeField] private Material m_Material;
        [SerializeField] private uint m_Tessellation = 0;
        [SerializeField] private Color32 m_Color = Color.white;
        [SerializeField] private bool m_Visible = true;
        
        [SerializeField] private Vector2 m_Size = new Vector2(1, 1);
        [SerializeField] private Vector2 m_Pivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField] private AutoSizeMode m_AutoSize = AutoSizeMode.Disabled;
        
        [SerializeField] private int m_SortingLayer = 0;
        [SerializeField] private int m_SortingOrder = 0;

        #endregion // Inspector

        [NonSerialized] private StreamingHelper.AwakeTracker m_Awake;
        [NonSerialized] private StreamingAssetHandle m_AssetHandle;
        [NonSerialized] private Texture m_LoadedTexture;
        [NonSerialized] private Mesh m_MeshInstance;
        [NonSerialized] private Shader m_LastKnownShader = null;
        [NonSerialized] private int m_MainTexturePropertyId = 0;
        [NonSerialized] private int m_MainColorPropertyId = 0;
        [NonSerialized] private Rect m_ClippedUVs;
        [NonSerialized] private ulong m_MeshInstanceHash;
 
        private readonly Streaming.AssetCallback OnAssetUpdated;

        private StreamingQuadTexture() {
            OnAssetUpdated = (StreamingAssetHandle id, Streaming.AssetStatus status, object asset) => {
                if (status == Streaming.AssetStatus.Loaded) {
                    m_LoadedTexture = (Texture) asset;
                    if (m_MainTexturePropertyId != 0) {
                        ApplyTextureAndColor();
                    }
                    ApplyVisible();
                    Resize(m_AutoSize);
                } else {
                    m_LoadedTexture = null;
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
            get { return m_Color; }
            set {
                if (m_Color != value) {
                    m_Color = value;
                    if (isActiveAndEnabled) {
                        ApplyColor();
                    }
                }
            }
        }

        /// <summary>
        /// Transparency of the renderer.
        /// </summary>
        public float Alpha {
            get { return m_Color.a / 255f; }
            set {
                if (m_Color.a != value) {
                    m_Color.a = (byte) (value * 255);
                    if (isActiveAndEnabled) {
                        ApplyColor();
                    }
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
        /// Sorting layer id for the renderer.
        /// </summary>
        public int SortingLayerId {
            get { return m_SortingLayer; }
            set {
                if (m_SortingLayer != value) {
                    m_SortingLayer = value;
                    if (isActiveAndEnabled) {
                        ApplySorting();
                    }
                }
            }
        }

        /// <summary>
        /// Sorting order for the renderer.
        /// </summary>
        public int SortingOrder {
            get { return m_SortingOrder; }
            set {
                if (m_SortingOrder != value) {
                    m_SortingOrder = value;
                    if (isActiveAndEnabled) {
                        ApplySorting();
                    }
                }
            }
        }
        
        /// <summary>
        /// Base material.
        /// </summary>
        public Material SharedMaterial {
            get { return m_Material; }
            set {
                if (m_Material != value) {
                    m_Material = value;
                    if (isActiveAndEnabled) {
                        LoadMaterial();
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
            get { return m_Size; }
            set {
                if (m_Size != value) {
                    m_Size = value;
                    Resize(m_AutoSize);
                }
            }
        }

        #endregion // Properties

        /// <summary>
        /// Resizes the mesh to preserve aspect ratio.
        /// </summary>
        public void Resize(AutoSizeMode sizeMode) {
            if (sizeMode == AutoSizeMode.Disabled || !m_LoadedTexture) {
                if (m_ClippedUVs != m_UVRect) {
                    m_ClippedUVs = m_UVRect;
                    if (isActiveAndEnabled) {
                        LoadMesh();
                    }
                }
                return;
            }

            Vector2 size = m_Size;
            Vector2 appliedPivot = m_Pivot;

            if (StreamingHelper.AutoSize(sizeMode, m_LoadedTexture, m_UVRect, transform.localPosition, m_Pivot, ref size, ref m_ClippedUVs, ref appliedPivot, StreamingHelper.GetParentSize(transform)) == 0) {
                return;
            }

            m_Size = size;

            if (isActiveAndEnabled) {
                LoadMesh();
            }
        }

        #region Unity Events

        private void Awake() {
            m_Awake.OnNaturalAwake();
        }

        private void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) {
                    return;
                }

                m_MeshFilter = GetComponent<MeshFilter>();
                m_MeshRenderer = GetComponent<MeshRenderer>();
                #if USING_BEAUUTIL
                m_ColorGroup = GetComponent<ColorGroup>();
                #endif // USING_BEAUUTIL
                if (m_ClippedUVs == default) {
                    m_ClippedUVs = m_UVRect;
                }
                Refresh();
                return;
            }
            #endif // UNITY_EDITOR

            if (m_ClippedUVs == default) {
                m_ClippedUVs = m_UVRect;
            }
            if (!m_Awake.IsForcing()) {
                Refresh();
            }
        }

        private void OnDisable() {
            Unload();
        }

        private void OnDestroy() {
            Unload();
        }

        [Preserve]
        private void OnDidApplyAnimationProperties() {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }

        #endregion // Unity Events

        #region Resources

        /// <summary>
        /// Prefetches
        /// </summary>
        public void Preload() {
            m_Awake.AwakeIfNotAwoken(this);
            LoadTexture();
            if (m_MainTexturePropertyId == 0) {
                LoadMaterial();
            }
            if (!m_MeshInstance) {
                LoadMesh();
            }
        }

        private void Refresh() {
            LoadMaterial();
            LoadTexture();
            ApplySorting();
            LoadMesh();
            ApplyVisible();
        }

        private void LoadMaterial() {
            if (!m_Material) {
                if (m_LastKnownShader != null) {
                    m_LastKnownShader = null;
                    m_MainTexturePropertyId = 0;
                    m_MainColorPropertyId = 0;
                }
                return;
            }

            m_MeshRenderer.sharedMaterial = m_Material;
            if (m_Material.shader != m_LastKnownShader) {
                m_LastKnownShader = m_Material.shader;
                m_MainTexturePropertyId = FindMainTexturePropertyName(m_LastKnownShader);

                // if we have a ColorGroup we shouldn't interfere with the color settings there
                #if USING_BEAUUTIL
                if (GetComponent<ColorGroup>()) {
                    m_MainColorPropertyId = 0;
                } else {
                    m_MainColorPropertyId = FindMainColorPropertyName(m_Material);
                }
                #else
                m_MainColorPropertyId = FindMainColorPropertyName(m_Material);
                #endif // USING_BEAUUTIL
            }

            ApplyTextureAndColor();
        }

        private void ApplyColor() {
            #if USING_BEAUUTIL
            if (m_ColorGroup != null) {
                m_ColorGroup.Color = m_Color;
                return;
            }
            #endif // USING_BEAUUTIL
            
            if (m_MainColorPropertyId != 0) {
                var spb = SharedPropertyBlock(m_MeshRenderer);
                spb.SetColor(m_MainColorPropertyId, m_Color);
                m_MeshRenderer.SetPropertyBlock(spb);
                spb.Clear();
            } else {
                LoadMesh();
            }
        }

        private void LoadTexture() {
            if (!Streaming.Texture(m_Path, ref m_AssetHandle, OnAssetUpdated)) {
                if (!m_AssetHandle) {
                    m_LoadedTexture = null;
                    m_MeshRenderer.enabled = false;
                    #if USING_BEAUUTIL
                    if (m_ColorGroup) {
                        m_ColorGroup.Visible = false;
                    }
                    #endif // USING_BEAUUTIL
                }
                return;
            }

            bool bHasTexture = m_LoadedTexture;

            m_MeshRenderer.enabled = bHasTexture && m_Visible;
            #if USING_BEAUUTIL
            if (m_ColorGroup) {
                m_ColorGroup.Visible = m_MeshRenderer.enabled;
            }
            #endif // USING_BEAUUTIL
            
            if (m_MainTexturePropertyId != 0) {
                ApplyTextureAndColor();
            }

            Streaming.AssetStatus status = Streaming.Status(m_AssetHandle);
            if ((status & Streaming.AssetStatus.Loaded) != 0) {
                Resize(m_AutoSize);
            } else {
                OnUpdated?.Invoke(this, status);
            }
        }

        private void ApplyTextureAndColor() {
            var spb = SharedPropertyBlock(m_MeshRenderer);
            if (m_LoadedTexture) {
                spb.SetTexture(m_MainTexturePropertyId, m_LoadedTexture);
                if (m_MainColorPropertyId != 0) {
                    spb.SetColor(m_MainColorPropertyId, m_Color);
                }
            } else {
                spb.SetTexture(m_MainTexturePropertyId, Texture2D.whiteTexture);
                if (m_MainColorPropertyId != 0) {
                    spb.SetColor(m_MainColorPropertyId, m_Color);
                }
            }
            m_MeshRenderer.SetPropertyBlock(spb);
            spb.Clear();
        }

        private void ApplySorting() {
            m_MeshRenderer.sortingLayerID = m_SortingLayer;
            m_MeshRenderer.sortingOrder = m_SortingOrder;
        }

        private void LoadMesh() {
            Color32 vertColor;
            if (m_MainColorPropertyId == 0) {
                vertColor = m_Color;
            } else {
                vertColor = Color.white;
            }

            m_MeshInstance = MeshGeneration.CreateQuad(m_Size, m_Pivot, vertColor, m_ClippedUVs, m_Tessellation, m_MeshInstance, ref m_MeshInstanceHash);
            m_MeshInstance.hideFlags = HideFlags.DontSave;
            m_MeshFilter.sharedMesh = m_MeshInstance;
        }

        private void ApplyVisible() {
            m_MeshRenderer.enabled = m_LoadedTexture && m_Visible;
            #if USING_BEAUUTIL
            if (m_ColorGroup) {
                m_ColorGroup.Visible = m_MeshRenderer.enabled;
            }
            #endif // USING_BEAUUTIL
        }

        /// <summary>
        /// Unloads all resources owned by the StreamingWorldTexture.
        /// </summary>
        public void Unload() {
            if (m_MeshRenderer) {
                m_MeshRenderer.enabled = false;
            }
            #if USING_BEAUUTIL
            if (m_ColorGroup) {
                m_ColorGroup.Visible = false;
            }
            #endif // USING_BEAUUTIL

            if (Streaming.Unload(ref m_AssetHandle, OnAssetUpdated)) {
                m_LoadedTexture = null;
                if (m_MainTexturePropertyId != 0) {
                    ApplyTextureAndColor();
                }
                OnUpdated?.Invoke(this, Streaming.AssetStatus.Unloaded);
            }
            StreamingHelper.DestroyResource(ref m_MeshInstance);
            m_MeshFilter.sharedMesh = null;
            m_MeshInstanceHash = 0;
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

        private void Reset() {
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            #if USING_BEAUUTIL
            m_ColorGroup = GetComponent<ColorGroup>();
            #endif // USING_BEAUUTIL

            m_Material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        private void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this)) {
                return;
            }

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            #if USING_BEAUUTIL
            m_ColorGroup = GetComponent<ColorGroup>();
            #endif // USING_BEAUUTIL

            if (this.isActiveAndEnabled) {
                EditorApplication.delayCall += () => {
                    if (!this || !this.isActiveAndEnabled) {
                        return;
                    }

                    if (!m_Material) {
                        m_Material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                    }

                    LoadMaterial();
                    LoadTexture();
                    ApplySorting();
                    Resize(m_AutoSize);
                    LoadMesh();
                    ApplyVisible();
                };
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor

        #region Utilities

        static private MaterialPropertyBlock s_SharedPropertyBlock;

        static internal MaterialPropertyBlock SharedPropertyBlock(MeshRenderer renderer) {
            MaterialPropertyBlock b = s_SharedPropertyBlock ?? (s_SharedPropertyBlock = new MaterialPropertyBlock());
            renderer.GetPropertyBlock(b);
            return b;
        }

        static internal int FindMainTexturePropertyName(Shader shader) {
            int propCount = shader.GetPropertyCount();
            for(int i = 0; i < propCount; i++) {
                ShaderPropertyFlags propertyFlags = shader.GetPropertyFlags(i);
                if ((propertyFlags & ShaderPropertyFlags.MainTexture) != 0) {
                    return shader.GetPropertyNameId(i); 
                }
            }
            return Shader.PropertyToID("_MainTex");
        }

        static internal int FindMainColorPropertyName(Material material) {
            Shader shader = material.shader;
            int propCount = shader.GetPropertyCount();
            for(int i = 0; i < propCount; i++) {
                ShaderPropertyFlags propertyFlags = shader.GetPropertyFlags(i);
                if ((propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.MainColor) != 0) {
                    return shader.GetPropertyNameId(i);
                }
            }

            int baseColor = Shader.PropertyToID("_BaseColor");
            int mainColor = Shader.PropertyToID("_MainColor");
            int color = Shader.PropertyToID("_Color");
            
            if (material.HasProperty(baseColor)) {
                return baseColor;
            } else if (material.HasProperty(mainColor)) {
                return mainColor;
            } else if (material.HasProperty(color)) {
                return color;
            } else {
                return 0;
            }
        }

        #endregion // Utilities
    }
}