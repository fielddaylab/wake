using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelEcosystemHeader : MonoBehaviour {
        #region Inspector

        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private LocText m_EnvironmentLabel = null;

        #endregion // Inspector

        private Routine m_ShowHideRoutine;

        private void Awake() {
            m_Group.Hide();
        }

        public void Show(BestiaryDesc environment) {
            m_ShowHideRoutine.Replace(this, m_Group.Show(0.2f)).ExecuteWhileDisabled();
            m_EnvironmentLabel.SetText(environment.CommonName());
        }

        public void Hide() {
            m_ShowHideRoutine.Replace(this, m_Group.Hide(0.2f));
        }
    }
}