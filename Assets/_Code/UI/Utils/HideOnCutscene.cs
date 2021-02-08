using System;
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

        private void OnEnable()
        {
            Services.Events.Register(GameEvents.CutsceneStart, OnCutsceneStart, this)
                .Register(GameEvents.CutsceneEnd, OnCutsceneEnd, this);

            if (Services.UI.IsLetterboxed())
            {
                OnCutsceneStart();
            }
            else
            {
                OnCutsceneEnd();
            }
        }

        private void OnDisable()
        {
            Services.Events?.Deregister(GameEvents.CutsceneStart, OnCutsceneStart)
                .Deregister(GameEvents.CutsceneEnd, OnCutsceneEnd);
        }

        private void OnCutsceneStart()
        {
            m_Group.alpha = 0;
            m_Group.blocksRaycasts = false;
        }

        private void OnCutsceneEnd()
        {
            m_Group.alpha = 1;
            m_Group.blocksRaycasts = true;
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Group = GetComponent<CanvasGroup>();
        }

        #endif // UNITY_EDITOR
    }
}