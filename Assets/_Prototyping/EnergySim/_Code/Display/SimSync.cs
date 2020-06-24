using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauUtil;
using System;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Energy
{
    public class SimSync : MonoBehaviour
    {
        public delegate void SyncChangedDelegate(float inSync);

        #region Inspector

        [SerializeField]
        private TMP_Text m_CurrentSyncDisplay = null;

        #endregion // Inspector

        [NonSerialized] private float m_LastKnownSync = -1;

        public event SyncChangedDelegate OnLocalSyncChanged;

        #region Unity Events

        private void Awake()
        {
        }

        #endregion // Unity Events

        public void Sync(in EnergySimContext inContextA, in EnergySimContext inContextB)
        {
            float error = 100 * EnergySim.CalculateError(inContextA.Current, inContextB.Current, inContextA.Database);
            float sync = 100 - (float) Math.Min(Math.Round(error), 100);

            UpdateLocalSync(sync);
        }

        private void UpdateLocalSync(float inSync)
        {
            if (m_LastKnownSync == inSync)
                return;
                
            m_LastKnownSync = inSync;
            m_CurrentSyncDisplay.text = inSync.ToString();

            OnLocalSyncChanged?.Invoke(inSync);
        }
    }
}