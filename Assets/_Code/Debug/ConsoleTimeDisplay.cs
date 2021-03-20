using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;

namespace Aqua.Debugging
{
    public class ConsoleTimeDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TMP_Text m_TimeScaleLabel = null;
        // [SerializeField] private TMP_Text m_StateLabel = null;

        #endregion // Inspector
        
        [NonSerialized] private float m_LastKnownTimescale;

        private void Awake()
        {
            UpdateTimescale(Time.timeScale);
        }

        public void UpdateTimescale(float inTimeScale)
        {
            if (m_LastKnownTimescale != inTimeScale)
            {
                m_LastKnownTimescale = inTimeScale;
                m_TimeScaleLabel.text = string.Format("{0:0.00}x", m_LastKnownTimescale);
            }
        }
    }
}