using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProtoAqua
{
    public class DemoTitleController : MonoBehaviour, ISceneLoadHandler
    {
        #region Inspector

        [SerializeField, Required] private Button m_ObservationButton = null;
        [SerializeField, Required] private Button m_ExperimentButton = null;
        [SerializeField, Required] private Button m_ModelingButton = null;
        [SerializeField, Required] private Button m_ArgumentationButton = null;
        [Space]
        [SerializeField, Required] private string m_ModelingData = null;

        #endregion // Inspector

        private void Awake()
        {
            m_ObservationButton.onClick.AddListener(OnClickObservationButton);
            m_ExperimentButton.onClick.AddListener(OnClickExperimentButton);
            m_ModelingButton.onClick.AddListener(OnClickModelingButton);
            m_ArgumentationButton.onClick.AddListener(OnClickArgumentationButton);
        }

        private void OnClickObservationButton()
        {
            Services.State.LoadScene("SeaSceneTest");
        }

        private void OnClickExperimentButton()
        {
            Services.State.LoadScene("ExperimentPrototype");
        }

        private void OnClickModelingButton()
        {
            QueryParams parm;
            QueryParams.TryParse(m_ModelingData, out parm);
            Services.State.LoadScene("SimScene", parm);
        }

        private void OnClickArgumentationButton()
        {
            Services.State.LoadScene("ArgumentationScene");
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            Services.Data.SetVariable("global:demo", true);
        }
    }
}