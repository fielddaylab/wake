using System.Runtime.InteropServices;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SimDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private SimOutput m_SimOutput = null;

        [SerializeField]
        private SimOutput m_DataOutput = null;

        [SerializeField]
        private SimTicker m_Ticker = null;

        [SerializeField]
        private SimSync m_Syncer = null;

        #endregion // Inspector

        public SimOutput Sim { get { return m_SimOutput; } }
        public SimOutput Data { get { return m_DataOutput; } }
        public SimTicker Ticker { get { return m_Ticker; } }
        public SimSync Syncer { get { return m_Syncer; } }

        public void Sync(in EnergySimContext inSimContext, in EnergySimContext inDataContext)
        {
            m_SimOutput?.Display(inSimContext);
            m_DataOutput?.Display(inDataContext);
            m_Ticker.Sync(inDataContext);
            m_Syncer.Sync(inSimContext, inDataContext);
        }
    }
}