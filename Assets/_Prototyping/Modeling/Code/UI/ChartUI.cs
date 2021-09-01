using System;
using System.Collections.Generic;
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
        [SerializeField, Required] private GraphAxis m_AxisWater = null;
        [SerializeField, Required] private LocText m_WaterLabel = null;

        [Header("Critters")]
        [SerializeField, Required] private GraphDisplay m_Historical = null;
        [SerializeField, Required] private GraphDisplay m_Player = null;
        [SerializeField, Required] private GraphDisplay m_Predict = null;
        [SerializeField, Required] private GraphDisplay m_Targets = null;

        [Header("Water Property")]
        [SerializeField, Required] private GraphDisplay m_HistoricalWater = null;
        [SerializeField, Required] private GraphDisplay m_PlayerWater = null;
        [SerializeField, Required] private GraphDisplay m_PredictWater = null;

        [Header("Chart Regions")]
        [SerializeField, Required] private RectTransform m_SyncRegion = null;
        [SerializeField, Required] private LocText m_SyncLabel = null;
        [SerializeField, Required] private RectTransform m_PredictRegion = null;
        [SerializeField, Required] private LocText m_PredictLabel = null;
        
        #endregion // Inspector

        [NonSerialized] private bool m_PredictMode;
        [NonSerialized] private WaterPropertyId m_WaterProp = WaterPropertyId.NONE;
        private readonly HashSet<StringHash32> m_GraphedCritters = new HashSet<StringHash32>();

        private readonly Predicate<StringHash32> CanGraphCritterPredicate;
        private readonly Predicate<StringHash32> CanGraphHistoricalCritterPredicate;
        private readonly Predicate<WaterPropertyId> CanGraphWaterPropPredicate;

        private Predicate<StringHash32> HasHistoricalPredicate;

        private ChartUI()
        {
            CanGraphCritterPredicate = CanGraphCritter;
            CanGraphHistoricalCritterPredicate = CanGraphHistoricalCritter;
            CanGraphWaterPropPredicate = CanGraphWaterProp;
        }

        private void Awake()
        {
            m_Predict.gameObject.SetActive(false);
            m_Targets.gameObject.SetActive(false);

            m_HistoricalWater.gameObject.SetActive(false);
            m_PlayerWater.gameObject.SetActive(false);
            m_WaterLabel.gameObject.SetActive(false);
            m_AxisWater.gameObject.SetActive(false);
        }

        public void ShowPrediction()
        {
            m_Predict.gameObject.SetActive(true);
            m_Targets.gameObject.SetActive(true);
            m_PredictWater.gameObject.SetActive(HasWaterProperty());
            m_PredictMode = true;
        }

        public void HidePrediction()
        {
            m_Predict.gameObject.SetActive(false);
            m_Targets.gameObject.SetActive(false);
            m_PredictWater.gameObject.SetActive(false);
            m_PredictMode = false;
        }

        public void Initialize(SimulationBuffer inBuffer)
        {
            m_GraphedCritters.Clear();
            foreach(var critter in inBuffer.PlayerCritters())
                m_GraphedCritters.Add(critter.Id());

            HasHistoricalPredicate = inBuffer.PlayerKnowsHistoricalPopulation;
        }

        public bool SetCritterGraphed(StringHash32 inCritterId, bool inbState)
        {
            if (inbState)
            {
                return m_GraphedCritters.Add(inCritterId);
            }
            else
            {
                return m_GraphedCritters.Remove(inCritterId);
            }
        }

        public void Refresh(SimulationBuffer inBuffer, SimulationBuffer.UpdateFlags inUpdate)
        {
            if (inUpdate == 0)
                return;
    
            if (Ref.Replace(ref m_WaterProp, inBuffer.Scenario().WaterProperty()))
            {
                if (HasWaterProperty())
                {
                    m_HistoricalWater.gameObject.SetActive(true);
                    m_PlayerWater.gameObject.SetActive(true);
                    m_WaterLabel.gameObject.SetActive(true);
                    m_WaterLabel.SetText(Services.Assets.WaterProp.Property(m_WaterProp).LabelId());
                    m_AxisWater.gameObject.SetActive(true);
                }
                else
                {
                    m_HistoricalWater.gameObject.SetActive(false);
                    m_PlayerWater.gameObject.SetActive(false);
                    m_WaterLabel.gameObject.SetActive(false);
                    m_AxisWater.gameObject.SetActive(false);
                }
            }

            if ((inUpdate & SimulationBuffer.UpdateFlags.Historical) != 0)
            {
                m_Historical.LoadCritters(inBuffer.HistoricalData(), inBuffer.Scenario(), CanGraphHistoricalCritterPredicate);
                m_Targets.LoadTargets(inBuffer.Scenario(), CanGraphCritterPredicate);

                if (HasWaterProperty())
                {
                    m_HistoricalWater.LoadProperty(inBuffer.HistoricalData(), m_WaterProp, inBuffer.Scenario(), CanGraphWaterPropPredicate);
                }
            }

            if ((inUpdate & SimulationBuffer.UpdateFlags.Model) != 0)
            {
                m_Player.LoadCritters(inBuffer.PlayerData(), inBuffer.Scenario(), CanGraphCritterPredicate);
                m_Predict.LoadCritters(inBuffer.PredictData(), inBuffer.Scenario(), CanGraphCritterPredicate);

                if (HasWaterProperty())
                {
                    m_PlayerWater.LoadProperty(inBuffer.PlayerData(), m_WaterProp, inBuffer.Scenario(), CanGraphWaterPropPredicate);
                    m_PredictWater.LoadProperty(inBuffer.PredictData(), m_WaterProp, inBuffer.Scenario(), CanGraphWaterPropPredicate);
                }
            }

            uint totalTicks = inBuffer.Scenario().TotalTicks();
            int tickScale = inBuffer.Scenario().TickScale();
            Rect critterRegion = RenderCritters(totalTicks, tickScale);
            RenderRegion(critterRegion, inBuffer);

            if (HasWaterProperty())
            {
                RenderWaterProperty(totalTicks, tickScale);
            }
        }

        private bool HasWaterProperty()
        {
            return m_WaterProp != WaterPropertyId.NONE;
        }

        private bool CanGraphCritter(StringHash32 inId)
        {
            return m_GraphedCritters.Contains(inId);
        }

        private bool CanGraphHistoricalCritter(StringHash32 inId)
        {
            return m_GraphedCritters.Contains(inId) && HasHistoricalPredicate(inId);
        }

        private bool CanGraphWaterProp(WaterPropertyId inId)
        {
            // TODO: toggles?
            return m_WaterProp == inId;
        }

        private Rect RenderCritters(uint inTotalTicks, int inTickScale)
        {
            GraphingUtils.AxisRangePair axisPair;
            if (m_PredictMode)
            {
                axisPair = CalculateGraphRect(m_Historical.Range, m_Player.Range, m_Predict.Range, m_Targets.Range, inTotalTicks, inTickScale, 8);
            }
            else
            {
                axisPair = CalculateGraphRect(m_Historical.Range, default(Rect), default(Rect), default(Rect), inTotalTicks, inTickScale, 8);
            }

            Rect fullRect = axisPair.ToRect();
            m_Historical.RenderLines(fullRect);
            m_Player.RenderLines(fullRect);
            m_Predict.RenderLines(fullRect);
            m_Targets.RenderLines(fullRect);

            m_Axis.Load(axisPair);

            return fullRect;
        }

        private Rect RenderWaterProperty(uint inTotalTicks, int inTickScale)
        {
            GraphingUtils.AxisRangePair axisPair;
            if (m_PredictMode)
            {
                axisPair = CalculateGraphRect(m_HistoricalWater.Range, m_PlayerWater.Range, m_PredictWater.Range, default(Rect), inTotalTicks, inTickScale, 8);
            }
            else
            {
                axisPair = CalculateGraphRect(m_HistoricalWater.Range, default(Rect), default(Rect), default(Rect), inTotalTicks, inTickScale, 8);
            }

            Rect fullRect = axisPair.ToRect();
            m_HistoricalWater.RenderLines(fullRect);
            m_PlayerWater.RenderLines(fullRect);
            m_PredictWater.RenderLines(fullRect);

            m_AxisWater.Load(axisPair);

            return fullRect;
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

        static private GraphingUtils.AxisRangePair CalculateGraphRect(in Rect inA, in Rect inB, in Rect inC, in Rect inD, uint inTickCountX, int inTickScale, uint inTickCountY)
        {
            Rect rect = inA;
            Geom.Encapsulate(ref rect, inB);
            Geom.Encapsulate(ref rect, inC);
            Geom.Encapsulate(ref rect, inD);

            GraphingUtils.AxisRangePair pair;
            pair.X = new GraphingUtils.AxisRangeInfo() { Min = 0, Max = inTickCountX * inTickScale, TickCount = inTickCountX + 1, TickInterval = 1 };
            pair.Y = GraphingUtils.CalculateAxis(rect.yMin, rect.yMax, inTickCountY);
            pair.Y.SetMinAtOrigin();
            return pair;
        }
    }
}