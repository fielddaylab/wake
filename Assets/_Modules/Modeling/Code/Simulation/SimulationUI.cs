using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using UnityEngine.UI;
using BeauUtil.Debugger;

namespace Aqua.Modeling {
    public class SimulationUI : BasePanel {
        private ModelState m_State;
        private ModelProgressInfo m_ProgressionInfo;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressionInfo = info;
        }

        #region Unity Events

        protected override void Awake() {
        }

        private void OnDestroy() {
        }

        #endregion // Unity Events

        #region BasePanel

        protected override void InstantTransitionToShow() {
            CanvasGroup.Show();
        }

        protected override void InstantTransitionToHide() {
            CanvasGroup.Hide();
        }

        protected override IEnumerator TransitionToShow() {
            return CanvasGroup.Show(0.2f);
        }

        protected override IEnumerator TransitionToHide() {
            return CanvasGroup.Hide(0.2f);
        }

        #endregion // BasePanel
    }
}