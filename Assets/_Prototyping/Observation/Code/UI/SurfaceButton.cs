using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;

namespace ProtoAqua.Observation
{
    public class SurfaceButton : SharedPanel
    {
        #region Inspector

        [SerializeField] private RectTransformPinned m_PinGroup = null;
        [SerializeField] private RectTransform m_AdjustGroup = null;

        #endregion // Inspector

        public void Display(Transform inTransform)
        {
            m_PinGroup.Pin(inTransform);
            Show();
        }

        #region Panel

        protected override void OnHideComplete(bool inbInstant)
        {
            m_PinGroup.Unpin();
            m_AdjustGroup.SetAnchorPos(16, Axis.Y);
            m_RootGroup.alpha = 0;

            base.OnHideComplete(inbInstant);
        }

        protected override IEnumerator TransitionToShow()
        {
            m_RootTransform.gameObject.SetActive(true);

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f).Ease(Curve.QuadOut),
                m_AdjustGroup.AnchorPosTo(0, 0.2f, Axis.Y).Ease(Curve.QuadOut)
            );
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                m_AdjustGroup.AnchorPosTo(16, 0.2f, Axis.Y).Ease(Curve.QuadIn),
                m_RootGroup.FadeTo(0, 0.2f).Ease(Curve.QuadIn)
            );

            m_RootTransform.gameObject.SetActive(false);
        }

        #endregion // Panel
    }
}