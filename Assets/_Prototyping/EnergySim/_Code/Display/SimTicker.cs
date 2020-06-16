using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauUtil;
using System;
using BeauRoutine;

namespace ProtoAqua.Energy
{
    public class SimTicker : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private TMP_Text m_CurrentTickCounter = null;

        [SerializeField]
        private TMP_Text m_MaxTickCounter = null;

        [SerializeField]
        private Slider m_Slider = null;

        [SerializeField]
        private RectTransformPool m_TickMarkPool = null;

        #endregion // Inspector

        [NonSerialized] private int m_LastMaxTicks = -1;

        #region Unity Events

        private void Awake()
        {
            m_TickMarkPool.Initialize();
        }

        #endregion // Unity Events

        public void Sync(in EnergySimContext inContext)
        {
            int currentTick = inContext.Current.Timestamp;
            int maxTicks = inContext.Scenario.TotalTicks();

            if (m_LastMaxTicks != maxTicks)
            {
                m_LastMaxTicks = maxTicks;
                m_MaxTickCounter.SetText(maxTicks.ToStringLookup());
                m_Slider.maxValue = maxTicks;
                InitTickMarks();
            }

            m_CurrentTickCounter.SetText(currentTick.ToStringLookup());

            m_Slider.SetValueWithoutNotify(currentTick);
        }

        private void InitTickMarks()
        {
            m_TickMarkPool.Reset();

            int ticksToAlloc = m_LastMaxTicks - 1;
            for(int i = 0; i < ticksToAlloc; ++i)
            {
                float pos = (float) (i + 1) / m_LastMaxTicks;
                RectTransform mark = m_TickMarkPool.InnerPool.Alloc();
                
                Vector2 min = mark.anchorMin, max = mark.anchorMax;
                min.x = max.x = pos;
                mark.anchorMin = min;
                mark.anchorMax = max;

                mark.SetAnchorPos(0, Axis.X);

                mark.gameObject.SetActive(true);
            }
        }
    }
}