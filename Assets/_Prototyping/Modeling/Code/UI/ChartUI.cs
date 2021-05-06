using System;
using Aqua;
using Aqua.Portable;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ChartUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private GraphAxis m_Axis = null;
        [SerializeField, Required] private GraphDisplay m_Historical = null;
        [SerializeField, Required] private GraphDisplay m_Player = null;
        [SerializeField, Required] private GraphDisplay m_Predict = null;
        [SerializeField, Required] private GraphPlot m_Targets = null;

        [Header("Chart Regions")]
        [SerializeField, Required] private RectTransform m_SyncRegion = null;
        [SerializeField, Required] private LocText m_SyncLabel = null;
        [SerializeField, Required] private RectTransform m_PredictRegion = null;
        [SerializeField, Required] private LocText m_PredictLabel = null;
        
        #endregion // Inspector

        [NonSerialized] private bool m_PredictMode;

        private void Awake()
        {
            m_Predict.gameObject.SetActive(false);
            m_Targets.gameObject.SetActive(false);
        }

        public void ShowPrediction()
        {
            m_Predict.gameObject.SetActive(true);
            m_Targets.gameObject.SetActive(true);
            m_PredictMode = true;
        }

        public void HidePrediction()
        {
            m_Predict.gameObject.SetActive(false);
            m_Targets.gameObject.SetActive(false);
            m_PredictMode = false;
        }

        public void Refresh(SimulationBuffer inBuffer, SimulationBuffer.UpdateFlags inUpdate)
        {
            if (inUpdate == 0)
                return;

            if ((inUpdate & SimulationBuffer.UpdateFlags.Historical) != 0)
            {
                m_Historical.LoadCritters(inBuffer.HistoricalData(), inBuffer.Scenario());
                m_Targets.LoadTargets(inBuffer.Scenario());
            }

            if ((inUpdate & SimulationBuffer.UpdateFlags.Model) != 0)
            {
                m_Player.LoadCritters(inBuffer.PlayerData(), inBuffer.Scenario());
                m_Predict.LoadCritters(inBuffer.PredictData(), inBuffer.Scenario());
            }

            uint totalTicks = inBuffer.Scenario().TotalTicks();

            GraphingUtils.AxisRangePair axisPair;
            if (m_PredictMode)
            {
                axisPair = CalculateGraphRect(m_Historical.Range, m_Player.Range, m_Predict.Range, m_Targets.Range, totalTicks, 8);
            }
            else
            {
                axisPair = CalculateGraphRect(m_Historical.Range, default(Rect), default(Rect), default(Rect), totalTicks, 8);
            }

            Rect fullRect = axisPair.ToRect();
            m_Historical.RenderLines(fullRect);
            m_Player.RenderLines(fullRect);
            m_Predict.RenderLines(fullRect);
            m_Targets.RenderPoints(fullRect);

            m_Axis.Load(axisPair);

            RenderRegion(fullRect, inBuffer);
        }

        private void RenderRegion(Rect inRect, SimulationBuffer inBuffer)
        {
            ModelingScenarioData scenario = inBuffer.Scenario();
            float divide = scenario.TickCount() * scenario.TickScale() / inRect.xMax;

            if (scenario.TickCount() > 0)
            {
                m_SyncRegion.anchorMax = new Vector2(divide, 1);
                m_SyncRegion.gameObject.SetActive(true);

                // TODO: Replace
                m_SyncLabel.SetText(string.Format("Last {0} years", scenario.TickCount()));
            }
            else
            {
                m_SyncRegion.gameObject.SetActive(false);
            }

            if (scenario.PredictionTicks() > 0)
            {
                m_PredictRegion.anchorMin = new Vector2(divide, 0);
                m_PredictRegion.gameObject.SetActive(true);

                // TODO: Replace
                m_PredictLabel.SetText(string.Format("Next {0} years", scenario.PredictionTicks()));
            }
            else
            {
                m_PredictRegion.gameObject.SetActive(false);
            }
        }

        static private GraphingUtils.AxisRangePair CalculateGraphRect(in Rect inA, in Rect inB, in Rect inC, in Rect inD, uint inTickCountX, uint inTickCountY)
        {
            Rect rect = inA;
            Geom.Encapsulate(ref rect, inB);
            Geom.Encapsulate(ref rect, inC);
            Geom.Encapsulate(ref rect, inD);

            GraphingUtils.AxisRangePair pair;
            pair.X = new GraphingUtils.AxisRangeInfo() { Min = 0, Max = inTickCountX, TickCount = inTickCountX + 1, TickInterval = 1 };
            pair.Y = GraphingUtils.CalculateAxis(rect.yMin, rect.yMax, inTickCountY);
            pair.Y.SetMinAtOrigin();
            return pair;
        }
    }
}