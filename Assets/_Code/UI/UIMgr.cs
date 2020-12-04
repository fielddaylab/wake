using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class UIMgr : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Camera m_UICamera = null;
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

        #region IService

        protected override void OnDeregisterService()
        {
        }

        protected override void OnRegisterService()
        {
            m_Loading.InstantShow();

            m_DialogStyleMap = new Dictionary<StringHash32, DialogPanel>(m_DialogStyles.Length);
            foreach(var panel in m_DialogStyles)
            {
                m_DialogStyleMap.Add(panel.StyleId(), panel);
            }
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.CommonUI;
        }

        #endregion // IService
    }
}