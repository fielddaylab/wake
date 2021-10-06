#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Scripting;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aqua
{
    public class UIMgr : ServiceBehaviour, IDebuggable
    {
        #region Inspector

        [SerializeField, Required] private Camera m_UICamera = null;

        [Header("Panels")]
        [SerializeField, Required] private DialogPanel m_DialogPanel = null;
        [SerializeField, Required] private PopupPanel m_PopupPanel = null;
        [SerializeField, Required] private DialogPanel[] m_DialogStyles = null;

        [Header("Overlays")]
        [SerializeField, Required] private LoadingDisplay m_Loading = null;
        [SerializeField, Required] private LetterboxDisplay m_Letterbox = null;
        [SerializeField, Required] private ScreenFaderDisplay m_WorldFaders = null;
        [SerializeField, Required] private ScreenFaderDisplay m_ScreenFaders = null;
        [SerializeField, Required] private FocusHighlight m_FocusHighlight = null;

        [Header("Input")]
        [SerializeField, Required] private InputCursor m_Cursor = null;
        [SerializeField, Required] private CursorTooltip m_Tooltip = null;
        [SerializeField, Required] private KeycodeDisplayMap m_KeyboardMap = null;
        [SerializeField] private float m_TooltipHoverTime = 0.6f;

        [Header("Flattening")]
        [SerializeField] private Transform[] m_HierarchiesToFlatten = null;

        #endregion // Inspector

        [NonSerialized] private int m_LetterboxCounter = 0;
        private Dictionary<StringHash32, DialogPanel> m_DialogStyleMap;
        private Dictionary<Type, SharedPanel> m_SharedPanels;
        [NonSerialized] private bool m_SkippingCutscene;
        [NonSerialized] private TempAlloc<FaderRect> m_SkipFader;
        [NonSerialized] private CursorHintMgr m_CursorHintMgr;

        #region Loading Screen

        public IEnumerator ShowLoadingScreen()
        {
            return m_Loading.Show();
        }

        public IEnumerator HideLoadingScreen()
        {
            return m_Loading.Hide();
        }

        public bool IsTransitioning()
        {
            return m_Loading.IsShowing() || m_Loading.IsTransitioning() || m_ScreenFaders.WipeCount > 0 || m_WorldFaders.WipeCount > 0;
        }

        public void ForceLoadingScreen()
        {
            m_Loading.InstantShow();
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
                m_SkipFader = null;
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

        #endregion // Additional Panels

        public InputCursor Cursor { get { return m_Cursor; } }
        public CursorHintMgr CursorHintMgr { get { return m_CursorHintMgr; } }
        public KeycodeDisplayMap KeycodeMap { get { return m_KeyboardMap; } }

        private void LateUpdate()
        {
            if (m_LetterboxCounter == 0 && m_Letterbox.IsShowing())
                m_Letterbox.Hide();

            m_CursorHintMgr.Process(m_TooltipHoverTime);
            Vector2 cursorPos = m_Cursor.Process();
            m_Tooltip.Process(cursorPos);
        }

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
        }

        #region IService

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

            m_SharedPanels = new Dictionary<Type, SharedPanel>(16);
            m_CursorHintMgr = new CursorHintMgr(m_Cursor, m_Tooltip);
            SceneHelper.OnSceneUnload += CleanupFromScene;

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

        [LeafMember("ShowPopup")]
        static private IEnumerator LeafShowPopup([BindContext] ScriptThread inThread, string inHeader, string inDescription)
        {
            inThread.Dialog = null;
            if (Services.UI.IsSkippingCutscene())
                return null;
            
            return Services.UI.Popup.Display(inHeader, inDescription, null).Wait();
        }

        #endregion // Leaf

        #if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            DMInfo uiMenu = new DMInfo("UI");

            RegisterDebugCutsceneToggle(uiMenu);

            uiMenu.AddButton("Clear All Faders", () => {
                m_WorldFaders.StopAll();
                m_ScreenFaders.StopAll();
            });

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