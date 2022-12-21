using UnityEngine;

namespace Aqua
{
    public class PopupLibrary : MonoBehaviour {
        [SerializeField] private JobCompletePopup m_JobComplete;

        private void OnEnable() {
            JobComplete = m_JobComplete;
        }

        private void Start() {
            m_JobComplete.gameObject.SetActive(false);
        }

        private void OnDisable() {
            JobComplete = null;
        }

        static public JobCompletePopup JobComplete { get; private set; }
    }
}