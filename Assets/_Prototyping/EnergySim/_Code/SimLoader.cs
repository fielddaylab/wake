using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BeauData;
using BeauUtil;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProtoAqua.Energy
{
    public class SimLoader : MonoBehaviour
    {
        static public readonly string Param_ScenarioId = "scenarioId";
        static public readonly string Param_ScenarioData = "scenarioData";

        #region Inspector

        [SerializeField] private EnergySimScenario[] m_Scenarios = null;
        [SerializeField] private EnergySimDatabase[] m_Databases = null;
        
        #endregion // Inspector

        [NonSerialized] private string[] m_ScenarioIds;
        [NonSerialized] private string[] m_DatabaseIds;

        #region Unity Events

        private void Awake()
        {
            m_ScenarioIds = new string[m_Scenarios.Length];
            m_DatabaseIds = new string[m_Databases.Length];

            for(int i = 0; i < m_Scenarios.Length; ++i)
            {
                var scenario = m_Scenarios[i];
                if (scenario != null)
                    m_ScenarioIds[i] = scenario.Id();
            }

            for(int i = 0; i < m_Databases.Length; ++i)
            {
                var database = m_Databases[i];
                if (database != null)
                    m_DatabaseIds[i] = database.Id();
            }
        }

        #endregion // Unity Events

        #region Scenarios

        public string[] ScenarioIds() { return m_ScenarioIds; }

        public ScenarioPackage LoadScenario(string inId)
        {
            int idx = Array.IndexOf(m_ScenarioIds, inId);
            if (idx < 0)
            {
                Debug.LogErrorFormat("Unknown scenario id '{0}'", inId);
                return null;
            }

            return m_Scenarios[idx].CreateRuntimePackage();
        }

        public ScenarioPackage LoadDefaultScenario()
        {
            return m_Scenarios[0].CreateRuntimePackage();
        }
        
        public ScenarioPackage LoadStartingScenario(QueryParams inParams)
        {
            ScenarioPackage package;
            if (!TryLoadScenarioFromParams(inParams, out package))
                package = LoadDefaultScenario();
            return package;
        }

        public bool TryLoadScenarioFromParams(QueryParams inParams, out ScenarioPackage outScenario)
        {
            if (inParams == null)
            {
                outScenario = null;
                return false;
            }

            string scenarioId = inParams.Get(Param_ScenarioId);
            if (!string.IsNullOrEmpty(scenarioId))
            {
                outScenario = LoadScenario(scenarioId);
                return outScenario != null;
            }

            string scenarioData = inParams.Get(Param_ScenarioData);
            return ScenarioPackage.TryParse(scenarioData, out outScenario);
        }
    
        #endregion // Scenarios

        #region Databases

        public string[] DatabaseIds() { return m_DatabaseIds; }

        public ISimDatabase LoadDatabase(string inId)
        {
            int idx = Array.IndexOf(m_DatabaseIds, inId);
            if (idx < 0)
            {
                Debug.LogErrorFormat("Unknown database id '{0}'", inId);
                return null;
            }

            return m_Databases[idx];
        }

        #endregion // Databases
    }
}