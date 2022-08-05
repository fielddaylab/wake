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

        private void Awake() {
            Button.onClick.AddListener(OnClick);
        }

        private void OnDisable() {
            m_ClickAnim.Stop();
            m_ActiveAnim.Stop();
            m_Visible = false;
        }

        public void SetVisible(bool visible, bool instant) {
            if (instant) {
                if (visible) {
                    InstantShow();
                } else {
                    InstantHide();
                }
                return;
            }

            if (visible) {
                m_Visible = true;
                Group.blocksRaycasts = false;
                m_ActiveAnim.Replace(this, EnableAnim()).ExecuteWhileDisabled();
            } else {
                m_Visible = false;
                Group.blocksRaycasts = false;
                m_ActiveAnim.Replace(this, DisableAnim()).ExecuteWhileDisabled();
            }
        }

        private IEnumerator EnableAnim() {
            gameObject.SetActive(true);
            Group.blocksRaycasts = false;
            yield return Rect.AnchorPosTo(0, 0.2f, Axis.X).Ease(Curve.Smooth);
            Group.blocksRaycasts = true;
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
            m_Visible = false;
            Group.blocksRaycasts = false;
            gameObject.SetActive(false);
            m_ActiveAnim.Stop();
        }

        private void InstantShow() {
            Rect.SetAnchorPos(0, Axis.X);
            m_Visible = true;
            Group.blocksRaycasts = true;
            gameObject.SetActive(true);
            m_ActiveAnim.Stop();
        }
    
        private void OnClick() {
            m_ClickAnim.Replace(this, ClickAnim());
        }

        private IEnumerator ClickAnim() {
            return Icon.AnchorPosTo(Icon.anchoredPosition.x + 8 * Direction, 0.1f, Axis.X).Yoyo().Ease(Curve.CubeOut).RevertOnCancel();
        }
    }
}