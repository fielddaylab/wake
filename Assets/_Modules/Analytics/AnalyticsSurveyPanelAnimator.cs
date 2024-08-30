using System.Collections;
using BeauRoutine;
using OGD;
using UnityEngine;

namespace Aqua.Analytics {
    [RequireComponent(typeof(SurveyPanel))]
    public class AnalyticsSurveyPanelAnimator : MonoBehaviour {
        public CanvasGroup FullGroup;
        public CanvasGroup QuestionGroup;

        private Routine m_FullFadeRoutine;

        private void Awake() {
            var panel = GetComponent<SurveyPanel>();
            panel.OnLoaded = OnLoaded;
            panel.OpenPageAnim = OpenPageAnim;
            panel.ClosePageAnim = ClosePageAnim;
            panel.FinishedAnim = FinishedAnim;

            FullGroup.alpha = 0;
            QuestionGroup.alpha = 0;
        }

        private void OnLoaded(SurveyPanel panel) {
            m_FullFadeRoutine.Replace(this, FullGroup.FadeTo(1, 0.3f));
        }

        private IEnumerator OpenPageAnim(SurveyPanel panel) {
            if (m_FullFadeRoutine) {
                yield return m_FullFadeRoutine.Wait();
            }
            yield return QuestionGroup.FadeTo(1, 0.2f);
        }

        private IEnumerator ClosePageAnim(SurveyPanel panel) {
            yield return QuestionGroup.FadeTo(0, 0.2f);
        }

        private IEnumerator FinishedAnim(SurveyPanel panel) {
            m_FullFadeRoutine.Stop();
            yield return FullGroup.FadeTo(0, 0.3f);
        }
    }
}