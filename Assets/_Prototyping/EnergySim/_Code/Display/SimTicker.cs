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
using Aqua;

namespace ProtoAqua.Energy
{
    public class SimTicker : MonoBehaviour
    {
        public delegate void TickChangedDelegate(ushort inTick);

        #region Inspector

        [SerializeField]
        private TMP_Text m_CurrentTickCounter = null;

        [SerializeField]
        private TMP_Text m_InitialTickCounter = null;

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

        [SerializeField]
        private Image m_ProgressMeter = null;

        #endregion // Inspector

        [NonSerialized] private int m_LastMaxTicks = -1;
        [NonSerialized] private int m_LastTick = -1;
        [NonSerialized] private Routine m_BufferedBroadcast;
        [NonSerialized] private float m_CurrentBufferDelay;

        private Routine m_ProgressMeterRoutine;

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
            UpdateTickText(currentTick);
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
                UpdateTickText(m_LastTick);
                OnTickChanged?.Invoke((ushort) m_LastTick);
            }

            yield break;
        }

        private void UpdateTickText(int inTick)
        {
            if (inTick <= 0 || inTick >= m_LastMaxTicks)
            {
                m_CurrentTickCounter.gameObject.SetActive(false);
                m_InitialTickCounter.SetAlpha(1);
                m_MaxTickCounter.SetAlpha(1);
            }
            else
            {
                m_CurrentTickCounter.gameObject.SetActive(true);
                m_CurrentTickCounter.SetText(inTick.ToStringLookup());

                float ratio = (float) inTick / m_LastMaxTicks;

                float firstAlpha = 1;
                if (ratio <= 0.25f)
                    firstAlpha = Curve.CubeIn.Evaluate(ratio * 4);
                float lastAlpha = 1;
                if (ratio >= 0.75f)
                    lastAlpha = Curve.CubeIn.Evaluate(1 - (4 * ratio - 3));

                m_InitialTickCounter.SetAlpha(firstAlpha);
                m_MaxTickCounter.SetAlpha(lastAlpha);
            }
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

            UpdateTickText(currentTick);
            m_Slider.SetValueWithoutNotify(currentTick);
            UpdateButtons();
        }

        public void UpdateTickSync(float[] inSyncs, float inPercentComplete)
        {
            var config = Services.Tweaks.Get<EnergyConfig>();
            if (inSyncs == null)
            {
                inSyncs = new float[m_LastMaxTicks + 1];
                for(int i = 0; i < inSyncs.Length; ++i)
                    inSyncs[i] = 100;
            }
            
            m_StartCap.color = config.EvaluateSyncGradientSubdued(inSyncs[0], false);
            for(int i = 0; i < m_TickMarkPool.ActiveObjects.Count; ++i)
            {
                m_TickMarkPool.ActiveObjects[i].GetComponent<Graphic>().color = config.EvaluateSyncGradientSubdued(inSyncs[i + 1], false);
            }
            m_EndCap.color = config.EvaluateSyncGradientSubdued(inSyncs[inSyncs.Length - 1], false);

            m_ProgressMeterRoutine.Replace(this, UpdateProgress(inPercentComplete, config.EvaluateSyncGradientProgress(inPercentComplete)));
        }

        private IEnumerator UpdateProgress(float inPercentComplete, Color inColor)
        {
            return Routine.Combine(
                m_ProgressMeter.FillTo(inPercentComplete / 100f, 0.2f).Ease(Curve.CubeOut),
                m_ProgressMeter.ColorTo(inColor, 0.2f).Ease(Curve.CubeOut)
            );
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