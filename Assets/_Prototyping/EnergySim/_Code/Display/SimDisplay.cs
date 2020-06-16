using System.Runtime.InteropServices;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SimDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private SimOutput m_CurrentOutput = null;

        [SerializeField]
        private SimTicker m_Ticker = null;

        #endregion // Inspector

        public void Sync(in EnergySimContext inContext)
        {
            m_CurrentOutput.Display(inContext);
            m_Ticker.Sync(inContext);
        }
    }
}