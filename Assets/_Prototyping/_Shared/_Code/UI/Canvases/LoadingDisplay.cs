using UnityEngine;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;

namespace ProtoAqua
{
    public class LoadingDisplay : BasePanel
    {
        #region Inspector

        [Header("Loading")]
        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private InputRaycasterLayer m_RaycastBlocker = null;

        [Header("Shared Elements")]
        [SerializeField] private CanvasGroup m_SharedGroup = null;
        [SerializeField] private SpriteAnimator m_SharedAnimator = null;
        [SerializeField] private TMP_Text m_SharedText = null;
        [SerializeField] private TweenSettings m_TransitionSettings = new TweenSettings(0.2f);
        
        [Header("Backgrounds")]
        [SerializeField] private CanvasGroup m_BackgroundGroup = null;

        #endregion // Inspector

        protected override void InstantTransitionToShow()
        {
            m_SharedGroup.alpha = 1;
            m_SharedGroup.gameObject.SetActive(true);

            m_BackgroundGroup.alpha = 1;
            m_BackgroundGroup.gameObject.SetActive(true);
        }

        protected override void InstantTransitionToHide()
        {
            m_SharedGroup.gameObject.SetActive(false);
            m_SharedGroup.alpha = 0;

            m_BackgroundGroup.gameObject.SetActive(false);
            m_BackgroundGroup.alpha = 0;
        }

        protected override void OnShow(bool inbInstant)
        {
            m_Canvas.enabled = true;
            m_RaycastBlocker.Override = null;
            if (!WasShowing())
            {
                Services.Input.PushPriority(m_RaycastBlocker);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_Canvas.enabled = false;
            m_RaycastBlocker.Override = false;

            if (WasShowing())
            {
                Services.Input?.PopPriority(m_RaycastBlocker);
            }
        }

        protected override IEnumerator TransitionToShow()
        {
            m_SharedGroup.gameObject.SetActive(true);
            m_BackgroundGroup.gameObject.SetActive(true);

            yield return Routine.Combine(
                m_SharedGroup.FadeTo(1, m_TransitionSettings),
                m_BackgroundGroup.FadeTo(1, m_TransitionSettings)
            );
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                m_SharedGroup.FadeTo(0, m_TransitionSettings),
                m_BackgroundGroup.FadeTo(0, m_TransitionSettings)
            );

            m_SharedGroup.gameObject.SetActive(false);
            m_BackgroundGroup.gameObject.SetActive(false);
        }
    }
}