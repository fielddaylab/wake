using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;

namespace ProtoAqua
{
    public partial class DebugService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private KeyCode m_ToggleMinimalKey = KeyCode.BackQuote;
        [SerializeField] private CanvasGroup m_MinimalLayer = null;

        #endregion // Inspector

        [NonSerialized] private bool m_MinimalOn;

        private void LateUpdate()
        {
            if (Input.GetKeyDown(m_ToggleMinimalKey))
            {
                SetMinimalLayer(!m_MinimalOn);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!Services.State.IsLoadingScene() && !Services.UI.IsLoadingScreenVisible() && !Services.UI.Popup.IsShowing())
                {
                    Routine.Start(this, RequestQuit());
                }
            }
        }

        private IEnumerator RequestQuit()
        {
            Future<StringHash> request = Services.UI.Popup.AskYesNo("Quit to Title Screen", "Return to the title screen?");
            yield return request;
            if (!request.Get().IsEmpty)
            {
                Services.Script.KillAllThreads();
                Services.UI.HideAll();
                Services.Audio.StopAll();
                Services.State.LoadScene("DebugTitle");
            }
        }

        private void SetMinimalLayer(bool inbOn)
        {
            m_MinimalOn = inbOn;
            m_MinimalLayer.alpha = inbOn ? 1 : 0;
            m_MinimalLayer.blocksRaycasts = inbOn;
        }

        #region IService

        protected override void OnRegisterService()
        {
            #if PREVIEW
            SetMinimalLayer(false);
            #else
            SetMinimalLayer(m_MinimalLayer.alpha > 0);
            #endif // PREVIEW
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.Debug;
        }

        #endregion // IService
    }
}