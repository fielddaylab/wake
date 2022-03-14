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
        [SerializeField] private LocText m_JobModelLabel = null;

        #endregion // Inspector

        private Routine m_ShowHideRoutine;

        private void Awake() {
            m_Group.Hide();
        }

        public void Show(BestiaryDesc environment, JobModelScope scope, bool hasJob) {
            m_ShowHideRoutine.Replace(this, m_Group.Show(0.2f)).ExecuteWhileDisabled();
            m_EnvironmentLabel.SetText(environment.CommonName());
            if (scope) {
                m_JobModelLabel.SetText("modeling.jobMode.label");
            } else if (hasJob) {
                m_JobModelLabel.SetText("modeling.wrongJobMode.label");
            } else {
                m_JobModelLabel.SetText("modeling.noJobMode.label");
            }
        }

        public void Hide() {
            m_ShowHideRoutine.Replace(this, m_Group.Hide(0.2f));
        }
    }
}