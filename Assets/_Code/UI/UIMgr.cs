#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Compression;
using Aqua.Scripting;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using Leaf;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aqua
{
    [DefaultExecutionOrder(10000), ServiceDependency(typeof(EventService))]
    public class UIMgr : ServiceBehaviour, IDebuggable
    {
        #region Inspector

        [SerializeField, Required] private Camera m_UICamera = null;

        [Header("Panels")]
        [SerializeField, Required] private DialogPanel m_DialogPanel = null;
        [SerializeField, Required] private PopupPanel m_PopupPanel = null;
        [SerializeField, Required] private DialogPanel[] m_DialogStyles = null;
        [SerializeField, Required] private LayoutPrefabPackage m_GenericLayouts = null;

        [Header("Overlays")]
        [SerializeField, Required] private LetterboxDisplay m_Letterbox = null;
        [SerializeField, Required] private ScreenFaderDisplay m_WorldFaders = null;
        [SerializeField, Required] private ScreenFaderDisplay m_ScreenFaders = null;
        [SerializeField, Required] private FocusHighlight m_FocusHighlight = null;

        [Header("Input")]
        [SerializeField, Required] private InputCursor m_Cursor = null;
        [SerializeField, Required] private CursorTooltip m_Tooltip = null;
        [SerializeField, Required] private KeycodeDisplayMap m_KeyboardMap = null;
        [SerializeField] private float m_TooltipHoverTime = 0.6f;

        [Header("Game UI")]
        [SerializeField] private string m_PersistentGameUIPath = null;
        [SerializeField] private string m_JournalUIPath = null;

        [Header("Flattening")]
        [SerializeField] private Transform[] m_HierarchiesToFlatten = null;

        #endregion // Inspector

        [NonSerialized] private int m_LetterboxCounter = 0;
        [NonSerialized] private int m_LetterboxDisableFrameCount = 0;
        private Dictionary<StringHash32, DialogPanel> m_DialogStyleMap;
        private Dictionary<Type, SharedPanel> m_SharedPanels;
        [NonSerialized] private bool m_SkippingCutscene;
        [NonSerialized] private TempAlloc<FaderRect> m_SkipFader;
        [NonSerialized] private CursorHintMgr m_CursorHintMgr;
        [NonSerialized] private List<GameObject> m_PersistentUIObjects = new List<GameObject>();
        [NonSerialized] private BufferedCollection<IUpdaterUI> m_UIUpdates = new BufferedCollection<IUpdaterUI>();
        [NonSerialized] private RingBuffer<KeyboardShortcutDisplay> m_ShortcutDisplays = new RingBuffer<KeyboardShortcutDisplay>(16, RingBufferMode.Expand);

        private Routine m_PersistentUILoad;
        private Routine m_JournalLoad;

        public Camera Camera { get { return m_UICamera; } }

        #region Loading Screen

        public bool IsTransitioning()
        {
            return  m_ScreenFaders.WipeCount > 0 || m_WorldFaders.WipeCount > 0;
        }

        #endregion // Loading Screen

        #region Dialog

        public DialogPanel Dialog { get { return m_DialogPanel; } }
        public PopupPanel Popup { get { return m_PopupPanel; } }

        public void HideAll()
        {
            m_DialogPanel.InstantHide();
            m_PopupPanel.InstantHide();
            m_LetterboxCounter = 0;
            m_Letterbox.InstantHide();
            m_ScreenFaders.StopAll();
            m_WorldFaders.StopAll();
            m_FocusHighlight.Hide(true);

            foreach(var panel in m_DialogStyles)
            {
                panel.InstantHide();
            }

            foreach(var panel in m_SharedPanels.Values)
            {
                panel.InstantHide();
            }
        }

        public DialogPanel GetDialog(StringHash32 inStyleId)
        {
            DialogPanel panel;
            if (!m_DialogStyleMap.TryGetValue(inStyleId, out panel))
            {
                panel = m_DialogPanel;
                Log.Error("[UIMgr] Unable to retrieve dialog panel with style '{0}'", inStyleId);
            }

            return panel;
        }

        public LayoutPrefabPackage CompressedLayouts {
            get { return m_GenericLayouts; }
        }

        #endregion // Dialog

        #region Letterbox

        public void ShowLetterbox()
        {
            if (++m_LetterboxCounter == 1)
                m_Letterbox.Show();
        }

        public void HideLetterbox()
        {
            if (m_LetterboxCounter > 0)
                --m_LetterboxCounter;
        }

        public bool IsLetterboxed()
        {
            return m_LetterboxCounter > 0;
        }

        public bool IsLetterboxVisible()
        {
            return m_LetterboxCounter > 0 || m_Letterbox.IsTransitioning();
        }

        public IEnumerator StartSkipCutscene()
        {
            if (!m_SkippingCutscene)
            {
                m_SkippingCutscene = true;
                m_SkipFader = m_ScreenFaders.AllocFader();
                return m_SkipFader.Object.Show(Color.black, 0.2f);
            }

            return null;
        }

        public void StopSkipCutscene()
        {
            if (m_SkippingCutscene)
            {
                m_SkipFader.Object?.Hide(0.2f);
                m_SkipFader = default;
                m_SkippingCutscene = false;
            }
        }

        public bool IsSkippingCutscene()
        {
            return m_SkippingCutscene;
        }

        #endregion // Letterbox

        #region Screen Effects

        public ScreenFaderDisplay ScreenFaders { get { return m_ScreenFaders; } }
        public ScreenFaderDisplay WorldFaders { get { return m_WorldFaders; } }
        public FocusHighlight Focus { get { return m_FocusHighlight; } }

        public ScreenFaderDisplay Faders(ScreenFaderLayer inLayer)
        {
            return inLayer == ScreenFaderLayer.Screen ? m_ScreenFaders : m_WorldFaders;
        }

        #endregion // Screen Effects

        #region Additional Panels

        public void RegisterPanel(SharedPanel inPanel)
        {
            Type t = inPanel.GetType();

            SharedPanel panel;
            if (m_SharedPanels.TryGetValue(t, out panel))
            {
                if (panel != inPanel)
                    throw new ArgumentException(string.Format("Panel with type {0} already exists", t.FullName), "inPanel");

                return;
            }

            m_SharedPanels.Add(t, inPanel);
        }

        public void DeregisterPanel(SharedPanel inPanel)
        {
            Type t = inPanel.GetType();

            SharedPanel panel;
            if (m_SharedPanels.TryGetValue(t, out panel) && panel == inPanel)
            {
                m_SharedPanels.Remove(t);
            }
        }

        public T FindPanel<T>() where T : SharedPanel
        {
            Type t = typeof(T);
            SharedPanel panel;
            if (!m_SharedPanels.TryGetValue(t, out panel))
            {
                panel = FindObjectOfType<T>();
                if (panel != null)
                {
                    RegisterPanel(panel);
                }
            }
            return (T) panel;
        }

        public bool TryFindPanel<T>(out T outPanel) where T : SharedPanel
        {
            Type t = typeof(T);
            SharedPanel panel;
            bool success = m_SharedPanels.TryGetValue(t, out panel);
            outPanel = panel as T;
            return success;
        }

        #endregion // Additional Panels

        #region Updates

        public void RegisterUpdate(IUpdaterUI updater) {
            m_UIUpdates.Add(updater);
        }

        public void DeregisterUpdate(IUpdaterUI updater) {
            m_UIUpdates.Remove(updater);
        }

        #endregion // Updates

        #region Shortcuts

        public void RegisterShortcut(KeyboardShortcutDisplay shortcut) {
            m_ShortcutDisplays.PushBack(shortcut);
            shortcut.SetDisplay(Accessibility.DisplayShortcuts);
        }

        public void DeregisterShortcut(KeyboardShortcutDisplay shortcut) {
            m_ShortcutDisplays.FastRemove(shortcut);
        }

        #endregion // Shortcuts

        #region Persistent UI

        public IEnumerator LoadPersistentUI()
        {
            if (m_PersistentUIObjects.Count == 0 && !m_PersistentUILoad)
            {
                return (m_PersistentUILoad = Routine.Start(this, LoadPersistentUI_Routine())).Wait();
            }

            return null;
        }

        private IEnumerator LoadPersistentUI_Routine()
        {
            using(Profiling.Time("loading persistent ui"))
            {
                Services.Assets.PreloadGroup("Prefab/Portable");
                var request = Future.Resources.LoadAsync<GameObject>(m_PersistentGameUIPath);
                yield return request;
                GameObject asset = request.Get();
                GameObject instantiated = Instantiate(asset);
                yield return null;
                DontDestroyOnLoad(instantiated);
                m_PersistentUIObjects.Capacity = instantiated.transform.childCount;
                
                foreach(Transform child in instantiated.transform)
                {
                    m_PersistentUIObjects.Add(child.gameObject);
                }
                instantiated.transform.FlattenHierarchy(false);
                GameObject.Destroy(instantiated);
            }
        }

        public void UnloadPersistentUI()
        {
            if (m_PersistentUILoad || m_PersistentUIObjects.Count > 0)
            {
                m_PersistentUILoad.Stop();
                foreach(var obj in m_PersistentUIObjects)
                {
                    GameObject.Destroy(obj);
                }

                m_PersistentUIObjects.Clear();
                Services.Assets.CancelPreload("Prefab/Portable");
            }
        }

        #endregion // Persistent UI

        #region Journal

        public void PreloadJournal()
        {
            JournalCanvas instance = FindPanel<JournalCanvas>();
            if (instance == null && !m_JournalLoad) {
                m_JournalLoad = Routine.Start(this, LoadJournalPrefab());
                Services.Assets.PreloadGroup(JournalCanvas.PreloadGroup);
            }
        }

        public bool IsJournalPreloaded()
        {
            return !m_JournalLoad && FindPanel<JournalCanvas>() && Services.Assets.PreloadGroupIsPrimaryLoaded(JournalCanvas.PreloadGroup);
        }

        public IEnumerator OpenJournalNewEntry()
        {
            JournalCanvas instance = FindPanel<JournalCanvas>();
            if (instance != null) {
                return instance.ShowNewEntry();
            } else {
                if (!m_JournalLoad) {
                    m_JournalLoad = Routine.Start(this, LoadJournalPrefab());
                }
                return DelayedJournalOperation((c) => c.ShowNewEntry());
            }
        }

        public IEnumerator OpenJournal() {
            JournalCanvas instance = FindPanel<JournalCanvas>();
            if (instance != null) {
                return instance.Show();
            } else {
                if (!m_JournalLoad) {
                    m_JournalLoad = Routine.Start(this, LoadJournalPrefab());
                }
                return DelayedJournalOperation((c) => c.Show());
            }
        }

        private IEnumerator LoadJournalPrefab()
        {
            using(Profiling.Time("loading journal ui"))
            {
                Services.Assets.PreloadGroup(JournalCanvas.PreloadGroup);
                var request = Future.Resources.LoadAsync<GameObject>(m_JournalUIPath);
                yield return request;
                GameObject asset = request.Get();
                GameObject instantiated = Instantiate(asset);
                JournalCanvas canvas = instantiated.GetComponentInChildren<JournalCanvas>(true);
                canvas.InstantHide();
                yield return AssetsService.PreloadHierarchy(instantiated);
            }
        }

        private IEnumerator DelayedJournalOperation(Action<JournalCanvas> onFinished) {
            using(Script.DisableInput()) {
                yield return m_JournalLoad;
                onFinished(FindPanel<JournalCanvas>());
            }
        }

        #endregion // Journal

        public InputCursor Cursor { get { return m_Cursor; } }
        public CursorHintMgr CursorHintMgr { get { return m_CursorHintMgr; } }
        public KeycodeDisplayMap KeycodeMap { get { return m_KeyboardMap; } }

        private void LateUpdate()
        {
            if (m_LetterboxCounter == 0 && m_Letterbox.IsShowing()) {
                if (++m_LetterboxDisableFrameCount > 1) {
                    m_Letterbox.Hide();
                }
            } else {
                m_LetterboxDisableFrameCount = 0;
            }

            m_CursorHintMgr.Process(m_TooltipHoverTime);
            Vector2 cursorPos = m_Cursor.Process();
            m_Tooltip.Process(cursorPos);

            m_UIUpdates.ForEach(UpdateUpdater);
        }

        static private readonly Action<IUpdaterUI> UpdateUpdater = (o) => {
            o.OnUIUpdate();
        };

        public void BindCamera(Camera inCamera)
        {
            var uiCameraData = m_UICamera.GetUniversalAdditionalCameraData();

            if (inCamera == null || inCamera == m_UICamera)
            {
                uiCameraData.renderType = CameraRenderType.Base;
                return;
            }

            uiCameraData.renderType = CameraRenderType.Overlay;
            var mainCameraData = inCamera.GetUniversalAdditionalCameraData();
            if (!mainCameraData.cameraStack.Contains(m_UICamera))
                mainCameraData.cameraStack.Add(m_UICamera);
            inCamera.cullingMask &= ~GameLayers.UI_Mask;
        }

        private void CleanupFromScene(SceneBinding inBinding, object inContext)
        {
            int removedPanelCount = 0;
            using(PooledList<SharedPanel> sharedPanels = PooledList<SharedPanel>.Create(m_SharedPanels.Values))
            {
                foreach(var panel in sharedPanels)
                {
                    if (panel.gameObject.scene == inBinding.Scene)
                    {
                        DeregisterPanel(panel);
                        ++removedPanelCount;
                    }
                }
            }

            if (removedPanelCount > 0)
            {
                Log.Warn("[UIMgr] Unregistered {0} shared panels that were not deregistered at scene unload", removedPanelCount);
            }

            if (m_JournalLoad) {
                m_JournalLoad.Stop();
                Services.Assets.CancelPreload(JournalCanvas.PreloadGroup);
            }
        }

        #region IService

        private void OnOptionsUpdated() {
            bool shortcutsEnabled = Accessibility.DisplayShortcuts;
            foreach(var shortcut in m_ShortcutDisplays) {
                shortcut.SetDisplay(shortcutsEnabled);
            }
        }

        protected override void Shutdown()
        {
            SceneHelper.OnSceneUnload -= CleanupFromScene;
        }

        protected override void Initialize()
        {
            m_DialogStyleMap = new Dictionary<StringHash32, DialogPanel>(m_DialogStyles.Length);
            foreach(var panel in m_DialogStyles)
            {
                m_DialogStyleMap.Add(panel.StyleId(), panel);
            }

            m_SharedPanels = new Dictionary<Type, SharedPanel>(16, ReferenceEqualityComparer<Type>.Default);
            m_CursorHintMgr = new CursorHintMgr(m_Cursor, m_Tooltip);
            SceneHelper.OnSceneUnload += CleanupFromScene;

            Services.Events.Register(GameEvents.OptionsUpdated, OnOptionsUpdated);

            BindCamera(Camera.main);
            transform.FlattenHierarchy();

            Async.InvokeAsync(FlattenHierarchyDynamic);
        }

        private void FlattenHierarchyDynamic()
        {
            foreach(var additionalTransform in m_HierarchiesToFlatten)
                additionalTransform.FlattenHierarchy();
        }

        #endregion // IService

        #region Leaf

        [LeafMember("ShowPopup"), UnityEngine.Scripting.Preserve]
        static private IEnumerator LeafShowPopup([BindThread] ScriptThread inThread, StringHash32 inHeader, StringHash32 inDescription)
        {
            inThread.Dialog = null;
            if (Services.UI.IsSkippingCutscene())
                return null;

            return Services.UI.Popup.Display(Loc.Find(inHeader), Loc.Find(inDescription), null).Wait();
        }

        #endregion // Leaf

        #if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus(FindOrCreateMenu findOrCreate)
        {
            DMInfo uiMenu = new DMInfo("UI");

            RegisterDebugCutsceneToggle(uiMenu);

            uiMenu.AddButton("Clear All Faders", () => {
                m_WorldFaders.StopAll();
                m_ScreenFaders.StopAll();
            });

            uiMenu.AddToggle("Unlock Guide", () => !Script.ReadVariable("world:hotbar.guide.locked").AsBool(), (b) => Script.WriteVariable("world:hotbar.guide.locked", !b), () => Services.Data.IsProfileLoaded());
            uiMenu.AddToggle("Unlock AQOS", () => !Script.ReadVariable("world:hotbar.portable.locked").AsBool(), (b) => Script.WriteVariable("world:hotbar.portable.locked", !b), () => Services.Data.IsProfileLoaded());

            yield return uiMenu;
        }

        static private void RegisterDebugCutsceneToggle(DMInfo inMenu)
        {
            bool bDebugSet = false;
            inMenu.AddToggle("Cutscene Mode", () => bDebugSet, 
            (b) => {
                bDebugSet = b;
                if (b)
                {
                    Services.UI.ShowLetterbox();
                }
                else
                {
                    Services.UI.HideLetterbox();
                }
            });
        }

        #endif // DEVELOPMENT
    }
}