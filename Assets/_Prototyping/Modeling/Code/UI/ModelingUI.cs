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
    public class ModelingUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private ModelingIntroUI m_Intro = null;
        [SerializeField] private ConceptMapUI m_ConceptMap = null;
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

        #endregion // Inspector

        [NonSerialized] private SimulationBuffer m_Buffer;
        [NonSerialized] private BaseInputLayer m_InputLayer;

        [NonSerialized] private RectTransform m_ChartTransform;

        public Action OnAdvanceClicked;

        private void Awake()
        {
            m_InputLayer = BaseInputLayer.Find(this);
            m_Chart.CacheComponent(ref m_ChartTransform);

            m_ModelSyncButton.onClick.AddListener(OnAdvanceButtonClicked);
            m_PredictSyncButton.onClick.AddListener(OnAdvanceButtonClicked);
        }
        
        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;

            m_ConceptMap.SetBuffer(inBuffer);
            m_InitialCritters.SetBuffer(inBuffer);
            m_CritterAdjust.SetBuffer(inBuffer);
        }

        public void Refresh(in ModelingState inState, SimulationBuffer.UpdateFlags inFlags)
        {
            m_Chart.Refresh(m_Buffer, inFlags);
            if (inFlags != 0)
            {
                m_ModelSync.Display(inState.ModelSync);
                m_PredictSync.Display(inState.PredictSync);
            }
        }

        public void ShowIntro()
        {
            m_PredictSync.gameObject.SetActive(false);
            m_ModelSync.gameObject.SetActive(true);
            m_CritterAdjust.gameObject.SetActive(false);
            m_InitialCritters.gameObject.SetActive(true);

            m_Intro.Load(m_Buffer.Scenario());
        }

        public void SwitchToPredict()
        {
            m_PredictSync.gameObject.SetActive(true);
            m_ModelSync.gameObject.SetActive(false);
            m_CritterAdjust.gameObject.SetActive(true);
            m_Chart.ShowPrediction();
            m_Chart.Refresh(m_Buffer, SimulationBuffer.UpdateFlags.Model);
            m_ConceptMap.Lock();

            Routine.Start(this, SwitchToPredictAnimation()).TryManuallyUpdate(0);
        }

        public void Complete()
        {
            m_ConceptMap.Lock();
            m_Complete.Load(m_Buffer.Scenario());
        }

        private IEnumerator SwitchToPredictAnimation()
        {
            Services.Input.PauseAll();
            yield return Routine.Combine(
                m_ChartTransform.AnchorPosTo(-m_ChartTransform.anchoredPosition.x, 0.5f, Axis.X).Ease(Curve.CubeInOut)
            );
            m_InitialCritters.gameObject.SetActive(false);
            Services.Input.ResumeAll();
        }

        private void OnAdvanceButtonClicked()
        {
            OnAdvanceClicked?.Invoke();
        }
    }
}