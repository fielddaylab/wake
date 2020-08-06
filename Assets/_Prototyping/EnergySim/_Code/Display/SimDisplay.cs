using System.Runtime.InteropServices;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SimDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private SimOutput m_DataOutput = null;

        [SerializeField]
        private SimOutput m_SimOutput = null;

        [SerializeField]
        private SimTicker m_Ticker = null;

        [SerializeField]
        private SimSync m_Syncer = null;

        [SerializeField]
        private RulesConfigPanel m_Rules = null;

        [SerializeField]
        private SimMenus m_Menus = null;

        #endregion // Inspector

        public SimOutput Sim { get { return m_SimOutput; } }
        public SimOutput Data { get { return m_DataOutput; } }
        public SimTicker Ticker { get { return m_Ticker; } }
        public SimSync Syncer { get { return m_Syncer; } }
        public RulesConfigPanel Rules { get { return m_Rules; } }
        public SimMenus Menus { get { return m_Menus; } }

        public void Sync(in ScenarioPackageHeader inHeader, in EnergySimContext inSimContext, in EnergySimContext inDataContext)
        {
            m_DataOutput?.Display(inHeader, inDataContext);
            m_SimOutput?.Display(inHeader, inSimContext, inDataContext.CachedCurrent);
            m_Ticker.Sync(inDataContext);
            m_Syncer.Sync(inSimContext, inDataContext);
        }
    }
}