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
        [SerializeField] private Image m_EnvironmentIcon = null;
        [SerializeField] private LocText m_JobModelLabel = null;
        [SerializeField] private LocText m_Status = null;

        #endregion // Inspector

        private Routine m_ShowHideRoutine;
        private Color m_DefaultStatusColor;

        private void Awake() {
            m_Group.Hide();
            m_DefaultStatusColor = m_Status.Graphic.color;
        }

        public void Show(BestiaryDesc environment, JobModelScope scope, bool hasJob) {
            m_ShowHideRoutine.Replace(this, m_Group.Show(0.2f)).ExecuteWhileDisabled();
            m_EnvironmentLabel.SetText(environment.CommonName());
            m_EnvironmentIcon.sprite = environment.Icon();
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

        public void SetStatusText(TextId text, Color? color) {
            m_Status.SetText(text);
            m_Status.Graphic.color = color.GetValueOrDefault(m_DefaultStatusColor);
        }
    }
}