using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;
using System;
using Aqua.Scripting;

namespace ProtoAqua.Navigation
{
    public class ResearchSite : MonoBehaviour
    {
        static public readonly StringHash32 Trigger_Found = "ResearchSiteFound";

        #region Inspector

        [SerializeField, Required] private string m_siteId = null;
        [SerializeField, Required] private string m_siteLabel = null;
        [SerializeField, Required] private Transform m_PlayerSpawnLocation = null;

        [Header("Components")]
        [SerializeField, Required] private ColorGroup m_RenderGroup = null;
        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Highlighted;

        public string SiteId { get { return m_siteId; } }
        public Transform PlayerSpawnLocation { get { return m_PlayerSpawnLocation; } }

        private void Awake()
        {
            var listener = m_Collider.EnsureComponent<TriggerListener2D>();
            listener.FilterByComponentInParent<PlayerController>();
            listener.onTriggerEnter.AddListener(OnPlayerEnter);
            listener.onTriggerExit.AddListener(OnPlayerExit);
        }

        public void CheckAllowed()
        {
            var currentJob = Services.Data.CurrentJob()?.Job;
            if (currentJob != null && currentJob.UsesDiveSite(m_siteId))
            {
                m_Highlighted = true;
                m_RenderGroup.SetAlpha(1);
            }
            else
            {
                m_Highlighted = false;
                m_RenderGroup.SetAlpha(0.25f);
            }
        }

        private void OnPlayerEnter(Collider2D other)
        {
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, m_siteId))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayDive(transform, m_siteLabel, m_siteId);
                
                using(var tempTable = TempVarTable.Alloc())
                {
                    tempTable.Set("siteId", m_siteId);
                    tempTable.Set("siteHighlighted", m_Highlighted);
                    Services.Script.TriggerResponse(Trigger_Found, null, null, tempTable);
                }
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (Services.Data && Services.Data.CompareExchange(GameVars.InteractObject, m_siteId, null))
            {
                Services.UI?.FindPanel<NavigationUI>()?.Hide();
            }
        }
    }

}
