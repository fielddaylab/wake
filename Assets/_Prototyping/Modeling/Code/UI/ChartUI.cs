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

        [SerializeField] private GraphAxis m_Axis = null;
        [SerializeField] private GraphDisplay m_Historical = null;
        [SerializeField] private GraphDisplay m_Player = null;
        [SerializeField] private GraphDisplay m_Predict = null;
        
        #endregion // Inspector

        public GraphAxis Axis { get { return m_Axis; } }
        public GraphDisplay Historical { get { return m_Historical; } }
        public GraphDisplay Player { get { return m_Player; } }
        public GraphDisplay Predict { get { return m_Predict; } }

        public bool Refresh(SimulationBuffer inBuffer)
        {
            bool bRectsChanged = false;

            if (inBuffer.RefreshHistorical())
            {
                m_Historical.LoadCritters(inBuffer.HistoricalData());
                bRectsChanged = true;
            }

            if (inBuffer.RefreshModel())
            {
                m_Player.LoadCritters(inBuffer.PlayerData());
                m_Predict.LoadCritters(inBuffer.PredictData());
                bRectsChanged = true;
            }

            if (bRectsChanged)
            {
                var axisPair = CalculateGraphRect(m_Historical.Range, m_Player.Range, m_Predict.Range, inBuffer.Scenario().TotalTicks(), 10);
                axisPair.X.SetMinAtOrigin();
                axisPair.Y.SetMinAtOrigin();
                Rect fullRect = axisPair.ToRect();
                m_Historical.RenderLines(fullRect);
                m_Player.RenderLines(fullRect);
                m_Predict.RenderLines(fullRect);
                m_Axis.Load(axisPair);
            }

            return bRectsChanged;
        }

        static private GraphingUtils.AxisRangePair CalculateGraphRect(Rect inA, Rect inB, Rect inC, uint inTickCountX, uint inTickCountY)
        {
            Rect rect = inA;
            Geom.Encapsulate(ref rect, inB);
            Geom.Encapsulate(ref rect, inC);
            return GraphingUtils.CalculateAxisPair(rect, inTickCountX, inTickCountY);
        }
    }
}