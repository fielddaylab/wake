using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProtoCP;

namespace ProtoAqua.Energy
{
    public class EditPanelTab : MonoBehaviour
    {
        #region Inspector

        [SerializeField] protected CPRoot m_PropertyBox = null;

        #endregion // Inspector

        [NonSerialized] protected ScenarioPackage m_Scenario;
        [NonSerialized] protected ISimDatabase m_Database;
        [NonSerialized] protected Action m_OnRuleRegen;
        [NonSerialized] protected Action m_OnScenarioUpdate;

        public virtual void Initialize(ScenarioPackage inPackage, ISimDatabase inDatabase, Action inOnRuleRegen, Action inOnScenarioUpdate)
        {
            m_Scenario = inPackage;
            m_Database = inDatabase;
            m_OnRuleRegen = inOnRuleRegen;
            m_OnScenarioUpdate = inOnScenarioUpdate;

            Populate();
        }

        protected virtual void Populate()
        {

        }

        protected virtual void DirtyScenario()
        {
            m_Scenario.Data.Dirty();
            m_OnScenarioUpdate?.Invoke();
        }

        protected virtual void DirtyDatabase()
        {
            m_Database.Dirty();
        }

        protected virtual void DirtyRules()
        {
            DirtyDatabase();
            m_OnRuleRegen?.Invoke();
        }
    }
}