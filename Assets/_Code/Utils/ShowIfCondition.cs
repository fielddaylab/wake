using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class ShowIfCondition : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_Conditions = null;
        [SerializeField] private GameObject[] m_ToShow = null;
        [SerializeField] private GameObject[] m_ToHide = null;
        [SerializeField, Tooltip("If set, this will be checked whenever game conditions are updated, or when a cutscene ends")] private bool m_ContinuousCheck = true;
        
        #endregion // Inspector

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private bool m_LastState;

        private void Awake()
        {
            if (m_ContinuousCheck)
            {
                Services.Events.Register(GameEvents.InventoryUpdated, RefreshBlockedByScript, this)
                    .Register(GameEvents.VariableSet, RefreshBlockedByScript, this)
                    .Register(GameEvents.CutsceneEnd, RefreshBlockedByScript, this);
            }

            Script.OnSceneLoad(Refresh);
        }

        private void OnDestroy()
        {
            if (!Services.Valid)
                return;

            Services.Events?.DeregisterAll(this);
        }

        private void RefreshBlockedByScript() {
            if (Script.IsLoading) {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            SetState(Services.Data.CheckConditions(m_Conditions));
            if (!m_ContinuousCheck)
                Destroy(this);
        }

        private void SetState(bool inbState)
        {
            if (m_Initialized && inbState == m_LastState)
                return;

            m_Initialized = true;
            m_LastState = inbState;
            foreach(var obj in m_ToShow)
                obj.SetActive(inbState);
            foreach(var obj in m_ToHide)
                obj.SetActive(!inbState);
        }
    }
}