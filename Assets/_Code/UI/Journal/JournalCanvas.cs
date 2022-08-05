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
        [SerializeField] private JournalPageButton m_BackButton = null;
        [SerializeField] private JournalPageButton m_NextButton = null;

        [Header("Tabs")]

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
        [NonSerialized] private int m_TotalSections = 0;
        [NonSerialized] private int m_CurrentSection = -1;
        [NonSerialized] private JournalCategoryMask m_AvailableTabs = 0;
        private Routine m_LoadRoutine;
        [NonSerialized] private float m_OriginalNavWidth;

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
            m_OriginalNavWidth = m_PageNavRect.sizeDelta.x;

            m_BackButton.Button.onClick.AddListener(OnBackClicked);
            m_NextButton.Button.onClick.AddListener(OnNextClicked);

            foreach(var tab in m_Tabs) {
                JournalTab cachedTab = tab;
                tab.Toggle.onValueChanged.AddListener((b) => OnTabToggle(cachedTab, b));
            }
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

            foreach(var tab in m_Tabs) {
                tab.gameObject.SetActive((tab.Category & m_AvailableTabs) == tab.Category);
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

            m_CurrentTab = mask;
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

            m_BackButton.SetVisible(sectionIdx > 0, IsTransitioning());
            m_NextButton.SetVisible(sectionIdx < m_TotalSections - 1, IsTransitioning());
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

            m_CurrentTab = 0;
            m_Tabs[0].Toggle.SetIsOnWithoutNotify(true);
            
            foreach(var tab in m_Tabs) {
                tab.AllowAnimation = false;
            }

            LoadList();
            FilterList(0);
        }

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShowComplete(inbInstant);

            foreach(var tab in m_Tabs) {
                tab.AllowAnimation = true;
            }
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
            CloseNavInstant();
            m_BGFader.alpha = 0;
            m_JournalTransform.SetAnchorPos(m_JournalOffscreenY, Axis.Y);
        }

        protected override void InstantTransitionToShow() {
            ShowNavInstant();
            m_BGFader.alpha = 1;
            m_JournalTransform.SetAnchorPos(0, Axis.Y);
        }

        protected override IEnumerator TransitionToHide() {
            Services.Audio.PostEvent("Journal.Close");
            yield return Routine.Combine(
                CloseNav(),
                m_JournalTransform.AnchorPosTo(m_JournalOffscreenY, 0.3f, Axis.Y).Ease(Curve.CubeIn).DelayBy(0.1f),
                m_BGFader.FadeTo(0, 0.3f).DelayBy(0.3f)
            );
            Root.gameObject.SetActive(false);
        }

        protected override IEnumerator TransitionToShow() {
            Services.Audio.PostEvent("Journal.Open");
            Root.gameObject.SetActive(true);
            yield return Routine.Combine(
                m_BGFader.FadeTo(1, 0.3f).DelayBy(0.1f),
                m_JournalTransform.AnchorPosTo(0, 0.3f, Axis.Y).Ease(Curve.BackOut),
                OpenNav(0.2f)
            );
        }

        private IEnumerator OpenNav(float delay) {
            yield return delay;
            m_PageNavRect.gameObject.SetActive(true);
            yield return m_PageNavRect.SizeDeltaTo(m_OriginalNavWidth, 0.3f, Axis.X).Ease(Curve.Smooth);
        }

        private IEnumerator CloseNav() {
            yield return m_PageNavRect.SizeDeltaTo(m_OriginalNavWidth - 200, 0.3f, Axis.X).Ease(Curve.Smooth);
            m_PageNavRect.gameObject.SetActive(false);
        }

        private void CloseNavInstant() {
            m_PageNavRect.gameObject.SetActive(false);
            m_PageNavRect.SetSizeDelta(m_OriginalNavWidth - 200, Axis.X);
        }

        private void ShowNavInstant() {
            m_PageNavRect.SetSizeDelta(m_OriginalNavWidth, Axis.X);
            m_PageNavRect.gameObject.SetActive(true);
        }

        #endregion // Animations

        #region Handlers

        private void OnNextClicked() {
            LoadSection(m_CurrentSection + 1, false);
        }

        private void OnBackClicked() {
            LoadSection(m_CurrentSection - 1, false);
        }

        private void OnTabToggle(JournalTab tab, bool state) {
            if (state && m_CurrentTab != tab.Category) {
                FilterList(tab.Category);
            }
        }

        #endregion // Handlers
    }
}