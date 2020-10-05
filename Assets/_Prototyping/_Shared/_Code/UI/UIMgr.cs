using System.Collections;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua
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

        #endregion // Inspector

        private int m_LetterboxCounter = 0;

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
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.CommonUI;
        }

        #endregion // IService
    }
}