using UnityEngine;
using BeauRoutine;
using BeauRoutine.Extensions;
using System.Collections;

namespace ProtoAqua
{
    public class LetterboxDisplay : BasePanel
    {
        #region Inspector

        [Header("Letterbox")]
        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private InputRaycasterLayer m_RaycastBlocker = null;
        [SerializeField] private RectTransform m_TopBar = null;
        [SerializeField] private RectTransform m_BottomBar = null;
        [SerializeField] private TweenSettings m_TransitionSettings = new TweenSettings(0.2f);

        #endregion // Inspector

        protected override void InstantTransitionToShow()
        {
            m_TopBar.gameObject.SetActive(true);
            m_BottomBar.gameObject.SetActive(true);

            m_TopBar.pivot = PivotTopAnchor;
            m_BottomBar.pivot = PivotBottomAnchor;
        }

        protected override void InstantTransitionToHide()
        {
            m_TopBar.gameObject.SetActive(false);
            m_BottomBar.gameObject.SetActive(false);

            m_TopBar.pivot = PivotBottomAnchor;
            m_BottomBar.pivot = PivotTopAnchor;
        }

        protected override void OnShow(bool inbInstant)
        {
            m_Canvas.enabled = true;
            m_RaycastBlocker.Override = null;
            
            if (!WasShowing())
            {
                Services.Input.PushPriority(m_RaycastBlocker);
                Services.Events.Dispatch(GameEvents.CutsceneStart);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
            if (WasShowing())
            {
                Services.Input?.PopPriority(m_RaycastBlocker);
                Services.Events.Dispatch(GameEvents.CutsceneEnd);
            }
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_Canvas.enabled = false;
            m_RaycastBlocker.Override = false;
        }

        protected override IEnumerator TransitionToShow()
        {
            m_TopBar.gameObject.SetActive(true);
            m_BottomBar.gameObject.SetActive(true);

            yield return Routine.Combine(
                m_TopBar.PivotTo(PivotTopAnchor, m_TransitionSettings),
                m_BottomBar.PivotTo(PivotBottomAnchor, m_TransitionSettings)
            );
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                m_TopBar.PivotTo(PivotBottomAnchor, m_TransitionSettings),
                m_BottomBar.PivotTo(PivotTopAnchor, m_TransitionSettings)
            );

            m_TopBar.gameObject.SetActive(false);
            m_BottomBar.gameObject.SetActive(false);
        }

        static private readonly Vector2 PivotTopAnchor = new Vector2(0.5f, 1);
        static private readonly Vector2 PivotBottomAnchor = new Vector2(0.5f, 0);
    }
}