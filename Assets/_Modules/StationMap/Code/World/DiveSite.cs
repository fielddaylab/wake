using UnityEngine;
using BeauUtil;
using System;
using Aqua.Scripting;

namespace Aqua.StationMap
{
    public class DiveSite : MonoBehaviour
    {
        static public readonly StringHash32 Trigger_Found = "DiveSiteFound";

        #region Inspector

        [SerializeField] private SerializedHash32 m_MapId = null;
        [SerializeField, Required] private Transform m_PlayerSpawnLocation = null;

        [Header("Components")]
        [SerializeField, Required] private ColorGroup m_RenderGroup = null;
        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Highlighted;

        public StringHash32 MapId { get { return m_MapId; } }
        public Transform PlayerSpawnLocation { get { return m_PlayerSpawnLocation; } }

        private void Awake()
        {
            var listener = m_Collider.EnsureComponent<TriggerListener2D>();
            listener.FilterByComponentInParent<PlayerController>();
            listener.onTriggerEnter.AddListener(OnPlayerEnter);
            listener.onTriggerExit.AddListener(OnPlayerExit);
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
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, m_MapId.Hash()))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayDive(transform, m_MapId);
                
                using(var tempTable = TempVarTable.Alloc())
                {
                    tempTable.Set("siteId", m_MapId.Hash());
                    tempTable.Set("siteHighlighted", m_Highlighted);
                    Services.Script.TriggerResponse(Trigger_Found, null, null, tempTable);
                }
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (Services.Data && Services.Data.CompareExchange(GameVars.InteractObject, m_MapId.Hash(), null))
            {
                Services.UI?.FindPanel<NavigationUI>()?.Hide();
            }
        }
    }

}
