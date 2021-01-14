using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;
using System;

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

        [NonSerialized] private bool m_Allowed;

        public string SiteId { get { return m_siteId; } }

        public void CheckAllowed()
        {
            var currentJob = Services.Data.CurrentJob()?.Job;
            if (currentJob != null && currentJob.UsesDiveSite(m_siteId))
            {
                m_Allowed = true;
                m_RenderGroup.SetAlpha(1);
            }
            else
            {
                m_Allowed = false;
                m_RenderGroup.SetAlpha(0.25f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!m_Allowed)
            {
                using(var tempTable = Services.Script.GetTempTable())
                {
                    tempTable.Set("siteId", m_siteId);
                    Services.Script.TriggerResponse("ResearchSiteLocked", "kevin", null, tempTable);
                    return;
                }
            }

            Services.UI.FindPanel<UIController>().Display(m_siteLabel, m_siteId);
             using(var tempTable = Services.Script.GetTempTable())
            {
                tempTable.Set("siteId", m_siteId);
                Services.Script.TriggerResponse("ResearchSiteFound", "kevin");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!m_Allowed)
                return;
            
            Services.UI.FindPanel<UIController>().Hide();
        }
    }

}
