using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Compression;
using Aqua.Journal;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public class JournalCanvas : SharedPanel, ISceneLoadHandler {
        static public readonly StringHash32 PreloadGroup = "Prefab/Journal";

        #region Type

        [Serializable] private class TextPool : SerializablePool<TMP_Text> { }
        [Serializable] private class RectGraphicPool : SerializablePool<RectGraphic> { }
        [Serializable] private class ImagePool : SerializablePool<Image> { }
        [Serializable] private class StreamingUGUIPool : SerializablePool<StreamingUGUITexture> { }
        [Serializable] private class LocTextPool : SerializablePool<LocText> { }

        private enum RequestType {
            None,
            NewEntry,
            NewEntryNewPage,
        }

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

        [SerializeField] private JournalPageGroup m_LeftPage = null;
        [SerializeField] private JournalPageGroup m_RightPage = null;

        [Header("Nav")]

        [SerializeField] private RectTransform m_PageNavRect = null;
        [SerializeField] private JournalPageButton m_BackButton = null;
        [SerializeField] private JournalPageButton m_NextButton = null;

        [Header("Tabs")]

        [SerializeField] private JournalTab[] m_Tabs = null;

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
        [NonSerialized] private RequestType m_RequestType;
        private Routine m_NewEntryRoutine;
        [NonSerialized] private bool m_InScene;
        [NonSerialized] private bool m_DeleteQueued;
        [NonSerialized] private bool m_HiddenTriggerQueued;

        static public bool Visible() {
            if (Services.UI.TryFindPanel<JournalCanvas>(out JournalCanvas c)) {
                return c.IsTransitioning() || c.IsShowing();
            } else {
                return false;
            }
        }

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
            m_Decompressor.ResourceCache = new Dictionary<ushort, UnityEngine.Object>(5);
        }

        protected override void Awake() {
            base.Awake();

            m_OriginalNavWidth = m_PageNavRect.sizeDelta.x;

            m_BackButton.Button.onClick.AddListener(OnBackClicked);
            m_NextButton.Button.onClick.AddListener(OnNextClicked);

            foreach(var tab in m_Tabs) {
                JournalTab cachedTab = tab;
                tab.Toggle.onValueChanged.AddListener((b) => OnTabToggle(cachedTab, b));
            }
        }

        protected override void OnDestroy() {
            if (!m_InScene && Services.Valid) {
                Services.Assets.CancelPreload(PreloadGroup);
            }

            m_Decompressor.ResourceCache.Clear();

            base.OnDestroy();
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
            if (m_RequestType == RequestType.NewEntry && m_TotalSections > 1 && (m_CurrentList.Count % 2) == 1) {
                m_RequestType = RequestType.NewEntryNewPage;
            }

            if (m_TotalSections == 0) {
                LoadSection(0, true);
            } else if (m_RequestType == RequestType.NewEntryNewPage) {
                LoadSection(m_TotalSections - 2, true);
            } else {
                LoadSection(m_TotalSections - 1, true);
            }
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
            int newIdx = -1;
            if (m_RequestType != RequestType.None && sectionIdx == m_TotalSections - 1) {
                newIdx = 1 - (m_CurrentList.Count % 2);
            }
            LoadPages(left, right, newIdx);

            m_BackButton.SetVisible(sectionIdx > 0, IsTransitioning());
            m_NextButton.SetVisible(sectionIdx < m_TotalSections - 1 && m_RequestType != RequestType.NewEntryNewPage, IsTransitioning());
        }

        private void LoadPages(JournalDesc left, JournalDesc right, int newIdx) {
            ResetPools();
            LoadPage(left, m_LeftPage);
            LoadPage(right, m_RightPage);
            m_LoadRoutine.Replace(this, LoadShowRoutine(newIdx));

            Streaming.UnloadUnusedAsync(30);
        }

        private void LoadPage(JournalDesc entry, JournalPageGroup page) {
            page.DisableMasking();

            if (entry == null) {
                page.Prefab.SetActive(false);
                page.Group.alpha = 0;
                return;
            }

            StringHash32 prefab = entry.PrefabId();

            page.Prefab.SetActive(false);
            m_CurrentDecompressionTarget = page.Prefab;
            m_CurrentLayouts.Decompress(prefab, m_Decompressor);
            m_CurrentDecompressionTarget = null;
            page.Group.alpha = 0;
            page.Prefab.SetActive(true);
        }

        private IEnumerator LoadShowRoutine(int newIdx) {
            while(IsTransitioning() || Streaming.IsLoading()) {
                yield return null;
            }

            if (newIdx == -1) {
                yield return Routine.Combine(
                    m_LeftPage.Group.FadeTo(1, 0.4f),
                    m_RightPage.Group.FadeTo(1, 0.4f).DelayBy(0.15f)
                );
            } else if (newIdx == 0) {
                yield return PageReveal(m_LeftPage);
            } else if (newIdx == 1) {
                yield return m_LeftPage.Group.FadeTo(1, 0.4f);
                yield return PageReveal(m_RightPage);
            }
        }

        private IEnumerator PageReveal(JournalPageGroup pageGroup) {
            yield return 0.3f;
            pageGroup.Group.alpha = 1;
            pageGroup.Mask.enabled = true;
            pageGroup.MaskImage.enabled = true;
            pageGroup.MaskImage.fillAmount = 0;
            Services.Audio.PostEvent("Journal.NewEntry");
            yield return pageGroup.MaskImage.FillTo(1, 1f);
            pageGroup.DisableMasking();
        }

        private IEnumerator NewEntryRoutine() {
            // show
            Services.Input.PauseAll();
            m_RequestType = RequestType.NewEntry;
            yield return Show();
            CanvasGroup.interactable = false;

            if (m_RequestType == RequestType.NewEntryNewPage) {
                yield return m_LoadRoutine;
                yield return 0.3f;
                LoadSection(m_CurrentSection + 1, false);
                Services.Audio.PostEvent("Journal.PageTurn");
                yield return 0.5f;
            }

            yield return m_LoadRoutine;
            yield return 0.5f;
            m_RequestType = RequestType.None;
            
            // wait for player to close
            Services.Input.ResumeAll();
            CanvasGroup.interactable = true;
            while(IsShowing() || IsTransitioning()) {
                yield return null;
            }
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

            m_CurrentLayouts = Services.Loc.CurrentJournalPackage;
            LoadList();
            FilterList(0);

            Services.Events.Queue(GameEvents.JournalOpen);
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
            m_RequestType = RequestType.None;

            if (!m_InScene && !inbInstant) {
                m_DeleteQueued = true;
            }

            m_HiddenTriggerQueued = !inbInstant;
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);

            m_InputLayer.PopPriority();
            m_InputLayer.Override = false;
            m_Canvas.enabled = false;
            ResetPools();
            m_LeftPage.DisableMasking();
            m_RightPage.DisableMasking();
            m_CurrentSection = -1;
            m_NewEntryRoutine.Stop();

            if (Services.Script != null && m_HiddenTriggerQueued) {
                Services.Script.TriggerResponse(GameTriggers.JournalHidden);
                Services.Events.Queue(GameEvents.JournalClosed);
            }

            if (m_DeleteQueued) {
                Async.InvokeAsync(() => Destroy(this.Canvas.gameObject));
            }
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

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            m_InScene = true;
        }

        #endregion // Handlers

        public IEnumerator ShowNewEntry() {
            m_NewEntryRoutine.Replace(this, NewEntryRoutine()).Tick();
            return m_NewEntryRoutine.Wait();
        }
    }
}