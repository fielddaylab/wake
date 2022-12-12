using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public class InlinePopupPanel : BasePanel {
        #region Inspector

        [Header("Contents")]
        [SerializeField] private PopupLayout m_Layout = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_BoxAnim;

        #region Initialization

        protected override void Awake() {
            base.Awake();
            m_Layout.Initialize();
            m_Layout.OnOptionSelected = (_) => Hide();
        }

        #endregion // Initialization

        #region Display

        public void Present(ref PopupContent inContent, PopupFlags inPopupFlags) {
            m_Layout.Configure(ref inContent, inPopupFlags);
            ShowOrBounce();
            SetInputState(true);
        }

        private void ShowOrBounce() {
            if (IsShowing()) {
                m_BoxAnim.Replace(this, BounceAnim());
                m_Layout.PlayAnim();
            } else {
                Show();
            }
        }

        public bool IsDisplaying() {
            return IsShowing();
        }

        #endregion // Display

        #region Animation

        private IEnumerator BounceAnim() {
            m_RootGroup.alpha = 0.5f;
            m_RootTransform.SetScale(0.5f);
            m_Layout.PlayAnim();
            yield return Routine.Combine(
                m_RootTransform.ScaleTo(1, 0.2f).ForceOnCancel().Ease(Curve.BackOut),
                m_RootGroup.FadeTo(1, 0.2f)
            );
        }

        protected override IEnumerator TransitionToShow() {
            float durationMultiplier = 1;
            if (m_RootGroup.alpha > 0 && m_RootGroup.alpha < 1)
                durationMultiplier = 0.5f;

            m_RootGroup.alpha = durationMultiplier < 1 ? 0.5f : 0;
            m_RootTransform.SetScale(durationMultiplier < 1 ? 0.75f : 0.5f);
            m_RootTransform.gameObject.SetActive(true);

            m_Layout.PlayAnim(0.15f);

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f * durationMultiplier),
                m_RootTransform.ScaleTo(1f, 0.2f * durationMultiplier).Ease(Curve.BackOut)
            );
        }

        protected override IEnumerator TransitionToHide() {
            yield return Routine.Combine(
                m_RootGroup.FadeTo(0, 0.15f),
                m_RootTransform.ScaleTo(0.5f, 0.15f).Ease(Curve.CubeIn)
            );

            m_RootTransform.gameObject.SetActive(false);
        }

        #endregion // Animation

        #region BasePanel

        protected override void OnHide(bool inbInstant) {
            SetInputState(false);

            m_BoxAnim.Stop();
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_RootGroup.alpha = 0;

            base.OnHideComplete(inbInstant);
        }

        #endregion // BasePanel
    }
}