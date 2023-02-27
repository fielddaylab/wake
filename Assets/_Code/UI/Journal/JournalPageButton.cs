using System;
using System.Collections;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public sealed class JournalPageButton : MonoBehaviour {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Button Button;
        public RectTransform Icon;
        [Range(-1, 1)] public int Direction = 1;

        [NonSerialized] private Routine m_ActiveAnim;
        [NonSerialized] private Routine m_ClickAnim;
        [NonSerialized] private bool m_Visible;
        [NonSerialized] private float m_OriginalIconX;

        private void Awake() {
            Button.onClick.AddListener(OnClick);
            m_OriginalIconX = Icon.anchoredPosition.x;
        }

        private void OnDisable() {
            m_ClickAnim.Stop();
            m_ActiveAnim.Stop();
        }

        public void SetVisible(bool visible, bool instant) {
            if (instant) {
                m_Visible = visible;
                if (visible) {
                    InstantShow();
                } else {
                    InstantHide();
                }
                return;
            }

            if (m_Visible == visible) {
                return;
            }

            m_Visible = visible;

            if (visible) {
                Group.blocksRaycasts = true;
                m_ActiveAnim.Replace(this, EnableAnim()).ExecuteWhileDisabled();
            } else {
                Group.blocksRaycasts = false;
                m_ActiveAnim.Replace(this, DisableAnim()).ExecuteWhileDisabled();
            }
        }

        private IEnumerator EnableAnim() {
            gameObject.SetActive(true);
            Group.blocksRaycasts = true;
            yield return Rect.AnchorPosTo(0, 0.2f, Axis.X).Ease(Curve.Smooth);
        }

        private IEnumerator DisableAnim() {
            if (m_ClickAnim) {
                yield return 0.1f;
            }
            yield return Rect.AnchorPosTo(-100f * Direction, 0.2f, Axis.X).Ease(Curve.Smooth);
            gameObject.SetActive(false);
        }

        private void InstantHide() {
            Rect.SetAnchorPos(-100f * Direction, Axis.X);
            gameObject.SetActive(false);
            m_ActiveAnim.Stop();
            Group.blocksRaycasts = false;
        }

        private void InstantShow() {
            Rect.SetAnchorPos(0, Axis.X);
            gameObject.SetActive(true);
            m_ActiveAnim.Stop();
            Group.blocksRaycasts = true;
        }
    
        private void OnClick() {
            m_ClickAnim.Replace(this, ClickAnim());
        }

        private IEnumerator ClickAnim() {
            return Icon.AnchorPosTo(m_OriginalIconX + 8 * Direction, 0.1f, Axis.X).Yoyo().Ease(Curve.CubeOut).RevertOnCancel();
        }
    }
}