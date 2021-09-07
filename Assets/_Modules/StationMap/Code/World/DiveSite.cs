using UnityEngine;
using BeauUtil;
using System;
using Aqua.Scripting;

namespace Aqua.StationMap
{
    public class DiveSite : ScriptComponent
    {
        static public readonly StringHash32 Trigger_Found = "DiveSiteFound";

        #region Inspector

        [SerializeField] private SerializedHash32 m_MapId = null;

        [Header("Components")]
        [SerializeField, Required] private ColorGroup m_RenderGroup = null;
        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Highlighted;

        public StringHash32 MapId { get { return m_MapId; } }

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);
        }

        public void CheckAllowed(JobDesc inCurrentJob)
        {
            if (inCurrentJob != null && inCurrentJob.UsesDiveSite(m_MapId))
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
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, m_MapId))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayDive(transform, m_MapId);
                
                using(var tempTable = TempVarTable.Alloc())
                {
                    tempTable.Set("siteId", m_MapId);
                    tempTable.Set("siteHighlighted", m_Highlighted);
                    Services.Script.TriggerResponse(Trigger_Found, tempTable);
                }
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (Services.Data && Services.Data.CompareExchange(GameVars.InteractObject, m_MapId, null))
            {
                Services.UI?.FindPanel<NavigationUI>()?.Hide();
            }
        }
    }

}
