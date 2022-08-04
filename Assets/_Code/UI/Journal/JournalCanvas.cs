using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Compression;
using Aqua.Journal;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public class JournalCanvas : SharedPanel {
        #region Type

        [Serializable] private class TextPool : SerializablePool<TMP_Text> { }
        [Serializable] private class RectGraphicPool : SerializablePool<RectGraphic> { }
        [Serializable] private class ImagePool : SerializablePool<Image> { }
        [Serializable] private class StreamingUGUIPool : SerializablePool<StreamingUGUITexture> { }
        [Serializable] private class LocTextPool : SerializablePool<LocText> { }

        #endregion // Type

        #region Inspector

        [Header("Basic Control")]

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private BaseInputLayer m_InputLayer = null;

        [Header("Animation")]

        [SerializeField] private CanvasGroup m_BGFader = null;
        [SerializeField] private RectTransform m_JournalTransform = null;
        [SerializeField] private float m_JournalOffscreenY = -650;

        [Header("Pools")]

        [SerializeField] private TextPool m_TextPool = null;
        [SerializeField] private RectGraphicPool m_RectGraphicPool = null;
        [SerializeField] private ImagePool m_ImagePool = null;
        [SerializeField] private StreamingUGUIPool m_StreamingUGUIPool = null;
        [SerializeField] private LocTextPool m_LocTextPool = null;

        [Header("Pages")]

        [SerializeField] private GameObject m_LeftPagePrefab = null;
        [SerializeField] private CanvasGroup m_LeftPageGroup = null;
        [SerializeField] private GameObject m_RightPagePrefab = null;
        [SerializeField] private CanvasGroup m_RightPageGroup = null;

        [Header("Nav")]

        [SerializeField] private RectTransform m_PageNavRect = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private Button m_NextButton = null;

        [Header("Tabs")]

        [SerializeField] private JournalTab m_SortingTab = null;
        [SerializeField] private JournalTab[] m_Tabs = null;

        [Header("Data")]

        [SerializeField] private LayoutPrefabPackage m_EnglishLayouts = null;

        #endregion // Inspector

        private PrefabDecompressor m_Decompressor;
        [NonSerialized] private GameObject m_CurrentDecompressionTarget;
        [NonSerialized] private LayoutPrefabPackage m_CurrentLayouts;
        [NonSerialized] private readonly List<JournalDesc> m_AllList = new List<JournalDesc>(32);
        [NonSerialized] private readonly List<JournalDesc> m_CurrentList = new List<JournalDesc>(32);
        [NonSerialized] private JournalCategoryMask m_CurrentTab = 0;
        [NonSerialized] private bool m_TabsOpen = false;
        [NonSerialized] private int m_TotalSections = 0;
        [NonSerialized] private int m_CurrentSection = -1;
        [NonSerialized] private JournalCategoryMask m_AvailableTabs = 0;
        private Routine m_LoadRoutine;

        private JournalCanvas() {
            m_Decompressor.NewRoot = (string name, CompressedPrefabFlags flags, CompressedComponentTypes componentTypes, GameObject parent) => {
                GameObject go = null;
                if ((flags & CompressedPrefabFlags.IsRoot) != 0) {
                    go = m_CurrentDecompressionTarget;
                } else if ((componentTypes & CompressedComponentTypes.LocText) != 0) {
                    go = m_LocTextPool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.RectGraphic) != 0) {
                    go = m_RectGraphicPool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.Image) != 0) {
                    go = m_ImagePool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.TextMeshPro) != 0) {
                    go = m_TextPool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.StreamingUGUITexture) != 0) {
                    go = m_StreamingUGUIPool.Alloc(parent.transform).gameObject;
                } else {
                    return null;
                }
                go.name = name;
                return go;
            };
            m_Decompressor.NewComponent = PrefabDecompressor.DefaultNewComponent;
        }

        protected override void Awake() {
            base.Awake();

            m_CurrentLayouts = m_EnglishLayouts;
        }

        #region Loading

        private void LoadList() {
            m_AllList.Clear();
            m_AvailableTabs = 0;
            foreach(var entryId in Save.Inventory.AllJournalEntryIds()) {
                var asset = Assets.Journal(entryId);
                m_AllList.Add(asset);
                m_AvailableTabs |= asset.Category();
            }
        }

        private void FilterList(JournalCategoryMask mask) {
            m_CurrentList.Clear();
            if (mask == 0) {
                m_CurrentList.AddRange(m_AllList);
            } else {
                foreach(var item in m_AllList) {
                    if ((item.Category() & mask) != 0) {
                        m_CurrentList.Add(item);
                    }
                }
            }

            m_TotalSections = (int) Math.Ceiling(m_CurrentList.Count / 2f);
            LoadSection(0, true);
        }

        private void ResetPools() {
            m_TextPool.Reset();
            m_ImagePool.Reset();
            m_LocTextPool.Reset();
            m_RectGraphicPool.Reset();
            m_StreamingUGUIPool.Reset();
        }

        private void LoadSection(int sectionIdx, bool force) {
            if (!force && sectionIdx == m_CurrentSection) {
                return;
            }

            m_CurrentSection = sectionIdx;
            int leftIdx = m_CurrentSection * 2;
            int rightIdx = leftIdx + 1;
            JournalDesc left = leftIdx < m_CurrentList.Count ? m_CurrentList[leftIdx] : null;
            JournalDesc right = rightIdx < m_CurrentList.Count ? m_CurrentList[rightIdx] : null;
            LoadPages(left, right);
        }

        private void LoadPages(JournalDesc left, JournalDesc right) {
            ResetPools();
            LoadPage(left, m_LeftPagePrefab, m_LeftPageGroup);
            LoadPage(right, m_RightPagePrefab, m_RightPageGroup);
            m_LoadRoutine.Replace(this, LoadShowRoutine());
        }

        private void LoadPage(JournalDesc entry, GameObject page, CanvasGroup group) {
            if (entry == null) {
                page.SetActive(false);
                group.alpha = 0;
                return;
            }

            StringHash32 prefab = entry.PrefabId();

            page.SetActive(false);
            m_CurrentDecompressionTarget = page;
            m_CurrentLayouts.Decompress(prefab, m_Decompressor);
            m_CurrentDecompressionTarget = null;
            group.alpha = 0;
            page.SetActive(true);
        }

        private IEnumerator LoadShowRoutine() {
            while(IsTransitioning() || Streaming.IsLoading()) {
                yield return null;
            }

            yield return Routine.Combine(
                m_LeftPageGroup.FadeTo(1, 0.4f),
                m_RightPageGroup.FadeTo(1, 0.4f).DelayBy(0.15f)
            );
        }

        #endregion // Loading

        #region Events

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_Canvas.enabled = true;
            m_InputLayer.PushPriority();
            m_InputLayer.Override = null;

            LoadList();
            FilterList(0);
        }

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            m_LoadRoutine.Stop();
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);

            m_InputLayer.PopPriority();
            m_InputLayer.Override = false;
            m_Canvas.enabled = false;
            ResetPools();
            m_CurrentSection = -1;
        }

        #endregion // Events

        #region Animations

        protected override void InstantTransitionToHide() {
            base.InstantTransitionToHide();
        }

        #endregion // Animations

        #region Handlers

        #endregion // Handlers
    }
}