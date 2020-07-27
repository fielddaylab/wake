using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauUtil;
using System;
using BeauRoutine;
using System.Collections;
using BeauRoutine.Extensions;

namespace ProtoAqua.Energy
{
    public class SimTicker : MonoBehaviour
    {
        public delegate void TickChangedDelegate(ushort inTick);

        #region Inspector

        [SerializeField]
        private TMP_Text m_CurrentTickCounter = null;

        [SerializeField]
        private TMP_Text m_MaxTickCounter = null;

        [SerializeField]
        private Slider m_Slider = null;

        [SerializeField]
        private RectTransformPool m_TickMarkPool = null;

        [SerializeField]
        private float m_ChangeBufferedDelay = 0.3f;

        [SerializeField]
        private Button m_PrevButton = null;

        [SerializeField]
        private Button m_NextButton = null;

        [SerializeField]
        private Graphic m_StartCap = null;

        [SerializeField]
        private Graphic m_EndCap = null;

        #endregion // Inspector

        [NonSerialized] private int m_LastMaxTicks = -1;
        [NonSerialized] private int m_LastTick = -1;
        [NonSerialized] private Routine m_BufferedBroadcast;
        [NonSerialized] private float m_CurrentBufferDelay;

        public event TickChangedDelegate OnTickChanged;

        #region Unity Events

        private void Awake()
        {
            m_TickMarkPool.Initialize();

            m_Slider.onValueChanged.AddListener(OnSliderValueChanged);
            m_PrevButton.onClick.AddListener(OnPrevButton);
            m_NextButton.onClick.AddListener(OnNextButton);
        }

        #endregion // Unity Events

        #region Handlers

        private void OnSliderValueChanged(float inNewValue)
        {
            if (!m_BufferedBroadcast)
            {
                m_BufferedBroadcast.Replace(this, DoBufferedBroadcast());
            }

            int currentTick = (int) inNewValue;

            m_CurrentBufferDelay = m_ChangeBufferedDelay;
            m_CurrentTickCounter.SetText(currentTick.ToStringLookup());
            UpdateButtons();
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator DoBufferedBroadcast()
        {
            while(m_CurrentBufferDelay > 0)
            {
                if (!Input.GetMouseButton(0))
                {
                    m_CurrentBufferDelay -= Routine.DeltaTime;
                }
                yield return null;
            }

            if (m_Slider.value != m_LastTick)
            {
                m_LastTick = (int) m_Slider.value;
                OnTickChanged?.Invoke((ushort) m_LastTick);
            }

            yield break;
        }

        #endregion // Routines

        public void Sync(in EnergySimContext inContext)
        {
            int currentTick = inContext.CachedCurrent.Timestamp;
            int maxTicks = inContext.Scenario.Data.TotalTicks();

            if (m_LastMaxTicks != maxTicks)
            {
                m_LastMaxTicks = maxTicks;
                m_MaxTickCounter.SetText(maxTicks.ToStringLookup());
                m_Slider.maxValue = maxTicks;
                InitTickMarks();
            }

            m_CurrentTickCounter.SetText(currentTick.ToStringLookup());

            m_Slider.SetValueWithoutNotify(currentTick);
            UpdateButtons();
        }

        public void UpdateTickSync(float[] inSyncs)
        {
            var config = Services.Tweaks.Get<EnergyConfig>();
            m_StartCap.color = config.EvaluateSyncGradientSubdued(inSyncs[0], false);
            for(int i = 0; i < m_TickMarkPool.ActiveObjects.Count; ++i)
            {
                m_TickMarkPool.ActiveObjects[i].GetComponent<Graphic>().color = config.EvaluateSyncGradientSubdued(inSyncs[i + 1], false);
            }
            m_EndCap.color = config.EvaluateSyncGradientSubdued(inSyncs[inSyncs.Length - 1], false);
        }

        private void InitTickMarks()
        {
            m_TickMarkPool.Reset();

            int ticksToAlloc = m_LastMaxTicks - 1;
            for(int i = 0; i < ticksToAlloc; ++i)
            {
                float pos = (float) (i + 1) / m_LastMaxTicks;
                RectTransform mark = m_TickMarkPool.Alloc();
                
                Vector2 min = mark.anchorMin, max = mark.anchorMax;
                min.x = max.x = pos;
                mark.anchorMin = min;
                mark.anchorMax = max;

                mark.SetAnchorPos(0, Axis.X);

                mark.gameObject.SetActive(true);
            }
        }

        private void UpdateButtons()
        {
            m_PrevButton.interactable = m_Slider.value > m_Slider.minValue;
            m_NextButton.interactable = m_Slider.value < m_Slider.maxValue;
        }

        private void OnPrevButton()
        {
            m_Slider.value -= 1;
        }

        private void OnNextButton()
        {
            m_Slider.value += 1;
        }
    }
}