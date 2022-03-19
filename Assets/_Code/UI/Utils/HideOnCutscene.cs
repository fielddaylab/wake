using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HideOnCutscene : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private CanvasGroup m_Group = null;

        #endregion // Inspector

        private Routine m_Anim;

        private readonly Action OnCutsceneStart;
        private readonly Action OnCutsceneEnd;

        private HideOnCutscene() {
            OnCutsceneStart = () => {
                m_Anim.Replace(this, Fade(0, false));
            };
            OnCutsceneEnd = () => {
                m_Anim.Replace(this, Fade(1, true));
            };
        }

        private void OnEnable()
        {
            Services.Events.Register(GameEvents.CutsceneStart, OnCutsceneStart, this)
                .Register(GameEvents.CutsceneEnd, OnCutsceneEnd, this);

            if (Services.UI.IsLetterboxed())
            {
                m_Group.alpha = 0;
                m_Group.blocksRaycasts = false;
            }
            else
            {
                m_Group.alpha = 1;
                m_Group.blocksRaycasts = true;
            }
        }

        private void OnDisable()
        {
            m_Anim.Stop();

            Services.Events?.Deregister(GameEvents.CutsceneStart, OnCutsceneStart)
                .Deregister(GameEvents.CutsceneEnd, OnCutsceneEnd);
        }

        private IEnumerator Fade(float inAlpha, bool inbRaycasts)
        {
            m_Group.blocksRaycasts = inbRaycasts;
            return m_Group.FadeTo(inAlpha, 0.1f);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Group = GetComponent<CanvasGroup>();
        }

        #endif // UNITY_EDITOR
    }
}