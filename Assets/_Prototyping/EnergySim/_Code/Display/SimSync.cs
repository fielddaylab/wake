using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauUtil;
using System;
using BeauRoutine;
using System.Collections;
using Aqua;

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

        #endregion // Inspector

        [NonSerialized] private float m_LastKnownSync = 0;

        public event SyncChangedDelegate OnLocalSyncChanged;

        public void Sync(in EnergySimContext inContextA, in EnergySimContext inContextB)
        {
            float localSync = Services.Tweaks.Get<EnergyConfig>().CalculateSync(inContextA, inContextB);
            UpdateLocalSync(localSync, false);
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
    }
}