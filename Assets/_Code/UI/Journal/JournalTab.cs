using System;
using System.Collections;
using Aqua.Journal;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public sealed class JournalTab : MonoBehaviour, IUpdaterUI {
        public RectTransform Rect;
        public Toggle Toggle;
        public JournalCategoryMask Category;
        public float SelectedOffset = 6;
        public bool AllowAnimation;

        [NonSerialized] private bool m_CurrentSelected;
        private Routine m_Anim;

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
            m_Anim.Stop();
            Rect.SetAnchorPos(0, Axis.X);
            Toggle.SetIsOnWithoutNotify(false);
            AllowAnimation = false;
            m_CurrentSelected = false;
        }

        public void OnUIUpdate() {
            if (AllowAnimation && m_CurrentSelected != Toggle.isOn) {
                m_CurrentSelected = Toggle.isOn;
                if (m_CurrentSelected) {
                    m_Anim.Replace(this, Transition(SelectedOffset, Curve.BackOut, true));
                } else {
                    m_Anim.Replace(this, Transition(0, Curve.CubeOut, false));
                }
            }
        }

        private IEnumerator Transition(float offset, Curve curve, bool audio) {
            if (audio) {
                Services.Audio.PostEvent("Journal.Tab");
            }
            yield return Rect.AnchorPosTo(offset, 0.1f, Axis.X).Ease(curve);
        }
    }
}