using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class UIMgr : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private DialogPanel m_DialogPanel = null;
        [SerializeField, Required] private PopupPanel m_PopupPanel = null;
        [SerializeField, Required] private LoadingDisplay m_Loading = null;
        [SerializeField, Required] private LetterboxDisplay m_Letterbox = null;
        [SerializeField, Required] private ScreenFaderDisplay m_WorldFaders = null;
        [SerializeField, Required] private ScreenFaderDisplay m_ScreenFaders = null;

        [SerializeField, Required] private DialogPanel[] m_DialogStyles = null;

        #endregion // Inspector

        private int m_LetterboxCounter = 0;
        private Dictionary<StringHash32, DialogPanel> m_DialogStyleMap;
        private Dictionary<StringHash32, SharedPanel> m_SharedPanels;

        #region Loading Screen

        public IEnumerator ShowLoadingScreen()
        {
            return m_Loading.Show();
        }

        public IEnumerator HideLoadingScreen()
        {
            return m_Loading.Hide();
        }

        public bool IsLoadingScreenVisible()
        {
            return m_Loading.IsShowing() || m_Loading.IsTransitioning();
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

            foreach(var panel in m_DialogStyles)
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
                Debug.LogErrorFormat("[UIMgr] Unable to retrieve dialog panel with style '{0}'", inStyleId.ToDebugString());
            }

            return panel;
        }

        #endregion // Dialog

        #region Letterbox

        public void ShowLetterbox()
        {
            if (++m_LetterboxCounter > 0)
                m_Letterbox.Show();
        }

        public void HideLetterbox()
        {
            if (m_LetterboxCounter > 0 && --m_LetterboxCounter == 0)
                m_Letterbox.Hide();
        }

        public bool IsLetterboxed()
        {
            return m_LetterboxCounter > 0;
        }

        #endregion // Letterbox

        #region Screen Effects

        public ScreenFaderDisplay ScreenFaders { get { return m_ScreenFaders; } }
        public ScreenFaderDisplay WorldFaders { get { return m_WorldFaders; } }

        public ScreenFaderDisplay Faders(ScreenFaderLayer inLayer)
        {
            return inLayer == ScreenFaderLayer.Screen ? m_ScreenFaders : m_WorldFaders;
        }

        #endregion // Screen Effects

        #region Additional Panels

        public void RegisterPanel(SharedPanel inPanel)
        {
            Type t = inPanel.GetType();
            StringHash32 key = t.FullName;

            SharedPanel panel;
            if (m_SharedPanels.TryGetValue(key, out panel))
            {
                if (panel != inPanel)
                    throw new ArgumentException(string.Format("Panel with type {0} already exists", t.FullName), "inPanel");

                return;
            }

            m_SharedPanels.Add(key, inPanel);
        }

        public void DeregisterPanel(SharedPanel inPanel)
        {
            Type t = inPanel.GetType();
            StringHash32 key = t.FullName;

            SharedPanel panel;
            if (m_SharedPanels.TryGetValue(key, out panel) && panel == inPanel)
            {
                m_SharedPanels.Remove(key);
            }
        }

        public T FindPanel<T>() where T : SharedPanel
        {
            StringHash32 key = typeof(T).FullName;
            SharedPanel panel;
            if (!m_SharedPanels.TryGetValue(key, out panel))
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
                Debug.LogWarningFormat("[UIMgr] Unregistered {0} shared panels that were not deregistered at scene unload", removedPanelCount);
            }
        }

        #region IService

        protected override void OnDeregisterService()
        {
            SceneHelper.OnSceneUnload -= CleanupFromScene;
        }

        protected override void OnRegisterService()
        {
            m_Loading.InstantShow();

            m_DialogStyleMap = new Dictionary<StringHash32, DialogPanel>(m_DialogStyles.Length);
            foreach(var panel in m_DialogStyles)
            {
                m_DialogStyleMap.Add(panel.StyleId(), panel);
            }

            m_SharedPanels = new Dictionary<StringHash32, SharedPanel>(16);

            SceneHelper.OnSceneUnload += CleanupFromScene;
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.CommonUI;
        }

        #endregion // IService
    }
}