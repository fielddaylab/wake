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

        [Header("Current Sync")]

        [SerializeField]
        private RectTransform m_CurrentSyncGroup = null;

        [SerializeField]
        private TMP_Text m_CurrentSyncDisplay = null;

        [SerializeField]
        private Image m_CurrentSyncBackground = null;

        [Header("Total Sync")]

        [SerializeField]
        private RectTransform m_TotalSyncGroup = null;

        [SerializeField]
        private TMP_Text m_TotalSyncDisplay = null;

        [SerializeField]
        private TMP_Text m_TotalSyncLoadingText = null;

        [SerializeField]
        private Image m_TotalSyncBackground = null;

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
            float localSync = Services.Tweaks.Get<EnergyConfig>().CalculateSync(inContextA, inContextB);
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
            m_CurrentSyncBackground.color = Services.Tweaks.Get<EnergyConfig>().EvaluateSyncGradientBold(inSync, false);
            
            Color textColor = Services.Tweaks.Get<EnergyConfig>().EvaluateSyncGradientBold(inSync, true);
            foreach(var text in m_CurrentSyncGroup.gameObject.GetComponentsInChildren<TMP_Text>())
            {
                text.color = textColor;
            }

            OnLocalSyncChanged?.Invoke(inSync);
        }

        private void UpdateTotalSync(float inSync, bool inbForce)
        {
            if (!inbForce && m_LastKnownTotalSync == inSync)
                return;
                
            Color textColor;
            m_LastKnownTotalSync = inSync;
            if (inSync >= 0)
            {
                m_TotalSyncDisplay.text = inSync.ToString();
                m_TotalSyncBackground.color = Services.Tweaks.Get<EnergyConfig>().EvaluateSyncGradientBold(inSync, false);
                m_TotalSyncLoadingText.gameObject.SetActive(false);
                m_TotalSyncDisplay.gameObject.SetActive(true);
                textColor = Services.Tweaks.Get<EnergyConfig>().EvaluateSyncGradientBold(inSync, true);
            }
            else
            {
                m_TotalSyncBackground.color = Services.Tweaks.Get<EnergyConfig>().SyncLoadingColor();
                m_TotalSyncLoadingText.gameObject.SetActive(true);
                m_TotalSyncDisplay.gameObject.SetActive(false);
                textColor = ColorBank.LightGray;
            }

            foreach(var text in m_CurrentSyncGroup.gameObject.GetComponentsInChildren<TMP_Text>())
            {
                text.color = textColor;
            }

            OnTotalSyncChanged?.Invoke(inSync);
        }
    }
}