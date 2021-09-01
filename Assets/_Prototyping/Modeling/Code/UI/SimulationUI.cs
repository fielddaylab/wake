using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class SimulationUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private RectTransform m_OutputPanel = null;
        [SerializeField] private BattleAnimationPrototype m_Battle = null;
        [SerializeField] private ChartUI m_Chart = null;
        [SerializeField] private ModelingCompleteUI m_Complete = null;
        
        [Header("Critter Lists")]
        [SerializeField] private InitialCritterUI m_InitialCritters = null;
        [SerializeField] private CritterAdjustUI m_CritterAdjust = null;
        
        [Header("Sync")]
        [SerializeField] private SyncDisplay m_ModelSync = null;
        [SerializeField] private SyncDisplay m_PredictSync = null;

        [Header("Buttons")]
        [SerializeField] private Button m_ModelSyncButton = null;
        [SerializeField] private Button m_PredictSyncButton = null;
        [SerializeField] private Button m_BackButton = null;

        #endregion // Inspector

        [NonSerialized] private SimulationBuffer m_Buffer;

        [NonSerialized] private float m_OriginalChartX;

        public Action OnAdvanceClicked;
        public Action OnBackClicked;

        private void Awake()
        {
            m_OriginalChartX = m_OutputPanel.anchoredPosition.x;

            m_ModelSyncButton.onClick.AddListener(OnAdvanceButtonClicked);
            m_PredictSyncButton.onClick.AddListener(OnAdvanceButtonClicked);
            m_BackButton.onClick.AddListener(OnBackButtonClicked);

            m_Battle.OnCritterToggled = OnCritterToggled;
        }
        
        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;

            m_InitialCritters.SetBuffer(inBuffer);
        }

        public void Refresh(in ModelingState inState, SimulationBuffer.UpdateFlags inFlags)
        {
            m_Chart.Refresh(m_Buffer, inFlags);

            if (inFlags != 0)
            {
                m_ModelSync.Display(inState.HasAllHistorical ? inState.ModelSync : (int?) null);
                m_PredictSync.Display(inState.PredictSync);

                if ((inFlags & SimulationBuffer.UpdateFlags.Model) != 0)
                {
                    switch(inState.Phase)
                    {
                        case ModelingPhase.Sync:
                            m_Battle.Animate(m_Buffer.PlayerData(), m_Buffer.PlayerDataDetails());
                            break;

                        case ModelingPhase.Predict:
                            m_Battle.Animate(m_Buffer.PredictData(), m_Buffer.PredictDataDetails());
                            break;
                    }
                }
            }
        }

        public void DisplayInitial()
        {
            m_PredictSync.gameObject.SetActive(false);
            m_ModelSync.gameObject.SetActive(true);

            m_CritterAdjust.gameObject.SetActive(false);
            m_InitialCritters.gameObject.SetActive(true);
            m_OutputPanel.SetAnchorPos(m_OriginalChartX, Axis.X);

            m_Chart.Initialize(m_Buffer);
            m_Chart.HidePrediction();

            m_Battle.SetBuffer(m_Buffer);
            m_Battle.ResetCritterToggles();
        }

        public void SwitchToPredict()
        {
            m_PredictSync.gameObject.SetActive(true);
            m_ModelSync.gameObject.SetActive(false);

            m_CritterAdjust.SetBuffer(m_Buffer);
            m_CritterAdjust.gameObject.SetActive(true);
            
            m_Chart.ShowPrediction();
            m_Chart.Refresh(m_Buffer, SimulationBuffer.UpdateFlags.Model);

            Routine.Start(this, SwitchToPredictAnimation()).TryManuallyUpdate(0);
        }

        public void Complete()
        {
            m_Battle.StopAnimation();
            m_Complete.Load(m_Buffer.Scenario());
        }

        private IEnumerator SwitchToPredictAnimation()
        {
            Services.Input.PauseAll();
            yield return Routine.Combine(
                m_OutputPanel.AnchorPosTo(-m_OriginalChartX, 0.5f, Axis.X).Ease(Curve.CubeInOut)
            );
            m_InitialCritters.gameObject.SetActive(false);
            Services.Input.ResumeAll();
        }

        private void OnAdvanceButtonClicked()
        {
            OnAdvanceClicked?.Invoke();
        }

        private void OnBackButtonClicked()
        {
            OnBackClicked?.Invoke();
        }

        private void OnCritterToggled(StringHash32 inCritterId, bool inbState)
        {
            if (m_Chart.SetCritterGraphed(inCritterId, inbState))
            {
                m_Chart.Refresh(m_Buffer, SimulationBuffer.UpdateFlags.ALL);
            }
        }
    }
}