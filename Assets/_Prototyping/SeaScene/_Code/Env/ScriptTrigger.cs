using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using Aqua;
using Aqua.Scripting;

namespace ProtoAqua.Observation
{
    public class ScriptTrigger : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_RegionId = null;
        [Space]
        [SerializeField] private Collider2D m_Collider = null;

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;

        private void Awake()
        {
            m_Listener = m_Collider.EnsureComponent<TriggerListener2D>();
            m_Listener.TagFilter.Add("Player");
            m_Listener.onTriggerEnter.AddListener(OnEnter);
            m_Listener.onTriggerExit.AddListener(OnExit);
        }

        private void OnEnter(Collider2D inCollider)
        {
            using(var table = TempVarTable.Alloc())
            {
                table.Set("regionId", m_RegionId);
                Services.Script.TriggerResponse(ObservationTriggers.PlayerEnterRegion, null, null, table);
            }
        }

        private void OnExit(Collider2D inCollider)
        {
            if (!Services.Script)
                return;
            
            using(var table = TempVarTable.Alloc())
            {
                table.Set("regionId", m_RegionId);
                Services.Script.TriggerResponse(ObservationTriggers.PlayerExitRegion, null, null, table);
            }
        }
    }
}