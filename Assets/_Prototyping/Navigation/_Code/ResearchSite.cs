using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;

namespace ProtoAqua.Navigation
{
    public class ResearchSite : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private string m_siteId = null;
        [SerializeField, Required] private string m_siteLabel = null;

        [Header("Components")]
        [SerializeField, Required] private ColorGroup m_RenderGroup = null;
        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        public string SiteId { get { return m_siteId; } }

        public void CheckAllowed()
        {
            var currentJob = Services.Data.CurrentJob()?.Job;
            if (currentJob != null && currentJob.UsesDiveSite(m_siteId))
            {
                m_Collider.enabled = true;
                m_RenderGroup.SetAlpha(1);
            }
            else
            {
                m_Collider.enabled = false;
                m_RenderGroup.SetAlpha(0.25f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Services.UI.FindPanel<UIController>().Display(m_siteLabel, m_siteId);
            // Debug.Log("Show Button");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Services.UI.FindPanel<UIController>().Hide();
            // Debug.Log("Hide Button");
        }
    }

}
