using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HideOnSceneLoad : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private CanvasGroup m_Group = null;

        #endregion // Inspector

        private Routine m_Anim;

        private readonly Action OnLoadStart;
        private readonly Action OnLoadEnd;

        private HideOnSceneLoad() {
            OnLoadStart = () => {
                m_Anim.Replace(this, Fade(0, false));
            };
            OnLoadEnd = () => {
                m_Anim.Replace(this, Fade(1, true));
            };
        }

        private void OnEnable()
        {
            Services.Events.Register(GameEvents.SceneWillUnload, OnLoadStart, this)
                .Register(GameEvents.SceneLoaded, OnLoadEnd, this);

            if (Script.IsLoading)
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

            Services.Events?.Deregister(GameEvents.SceneWillUnload, OnLoadStart)
                .Deregister(GameEvents.SceneLoaded, OnLoadEnd);
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