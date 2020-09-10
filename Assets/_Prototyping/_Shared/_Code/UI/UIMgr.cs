using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public class UIMgr : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private Camera m_UICamera = null;
        [SerializeField] private DialogPanel m_DialogPanel = null;
        [SerializeField] private PopupPanel m_PopupPanel = null;
        [SerializeField] private LoadingDisplay m_Loading = null;
        [SerializeField] private LetterboxDisplay m_Letterbox = null;

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

        #endregion // Loading Screen

        #region Dialog

        public DialogPanel Dialog() { return m_DialogPanel; }
        public PopupPanel Popup() { return m_PopupPanel; }

        public void HideAll()
        {
            m_DialogPanel.InstantHide();
            m_PopupPanel.InstantHide();
            m_LetterboxCounter = 0;
            m_Letterbox.InstantHide();
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

        #endregion // Letterbox

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