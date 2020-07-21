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

        [SerializeField]
        private Image m_CurrentSyncBackground = null;

        [SerializeField]
        private TMP_Text m_TotalSyncDisplay = null;

        [SerializeField]
        private TMP_Text m_TotalSyncLoadingText = null;

        [SerializeField]
        private Image m_TotalSyncBackground = null;

        [SerializeField]
        private Gradient m_SyncGradient = null;

        [SerializeField]
        private Color m_TotalSyncLoadingColor = Color.gray;

        [SerializeField]
        private float m_ErrorScale = 1;

        #endregion // Inspector

        [NonSerialized] private float m_LastKnownSync = 0;
        [NonSerialized] private float m_LastKnownTotalSync = 0;

        public event SyncChangedDelegate OnLocalSyncChanged;
        public event SyncChangedDelegate OnTotalSyncChanged;

        #region Unity Events

        private void Awake()
        {
            UpdateTotalSync(-1, true);
        }

        #endregion // Unity Events

        public void Sync(in EnergySimContext inContextA, in EnergySimContext inContextB)
        {
            float localSync = CalculateSync(inContextA, inContextB);
            UpdateLocalSync(localSync, false);
        }

        public void HideTotalSync()
        {
            UpdateTotalSync(-1, true);
        }

        public void ShowTotalSync(float inSync)
        {
            UpdateTotalSync(inSync, false);
        }

        private void UpdateLocalSync(float inSync, bool inbForce)
        {
            if (!inbForce && m_LastKnownSync == inSync)
                return;
                
            m_LastKnownSync = inSync;
            m_CurrentSyncDisplay.text = inSync.ToString();
            m_CurrentSyncBackground.color = m_SyncGradient.Evaluate(inSync / 100f);

            OnLocalSyncChanged?.Invoke(inSync);
        }

        private void UpdateTotalSync(float inSync, bool inbForce)
        {
            if (!inbForce && m_LastKnownTotalSync == inSync)
                return;
                
            m_LastKnownTotalSync = inSync;
            if (inSync >= 0)
            {
                m_TotalSyncDisplay.text = inSync.ToString();
                m_TotalSyncBackground.color = m_SyncGradient.Evaluate(inSync / 100f);
                m_TotalSyncLoadingText.gameObject.SetActive(false);
                m_TotalSyncDisplay.gameObject.SetActive(true);
            }
            else
            {
                m_TotalSyncBackground.color = m_TotalSyncLoadingColor;
                m_TotalSyncLoadingText.gameObject.SetActive(true);
                m_TotalSyncDisplay.gameObject.SetActive(false);
            }

            OnTotalSyncChanged?.Invoke(inSync);
        }

        public float CalculateSync(in EnergySimContext inContextA, in EnergySimContext inContextB)
        {
            float error = 100 * EnergySim.CalculateError(inContextA.CachedCurrent, inContextB.CachedCurrent, inContextA.Database);
            float sync = 100 - (float) Math.Min(Math.Round(error * m_ErrorScale), 100);
            return sync;
        }
    }
}