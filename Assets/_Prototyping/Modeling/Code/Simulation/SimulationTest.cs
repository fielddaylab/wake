using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class SimulationTest : MonoBehaviour, ISceneLoadHandler
    {
        [SerializeField] private ModelingScenarioData m_TestScenario = null;
        [SerializeField] private TempModelingUI m_UI = null;
        [SerializeField] private ConceptMap m_ConceptMap = null;
        [SerializeField] private GraphDisplay m_HistoricalDisplay = null;
        [SerializeField] private GraphDisplay m_PlayerDisplay = null;

        [NonSerialized] private SimulationBuffer m_Buffer;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            #if UNITY_EDITOR

            void LogThing<T>()
            {
                Debug.LogFormat("sizeof({0})={1}", typeof(T).Name, System.Runtime.InteropServices.Marshal.SizeOf<T>());
            }

            LogThing<WaterPropertyBlockF32>();
            LogThing<CritterData>();

            #endif // UNITY_EDITOR

            m_Buffer = new SimulationBuffer();
            m_Buffer.SetScenario(m_TestScenario);
            m_UI.SetBuffer(m_Buffer, Refresh);

            Refresh();
        }

        private void Refresh()
        {
            bool bRectsChanged = false;

            if (m_Buffer.RefreshHistorical())
            {
                m_HistoricalDisplay.LoadCritters(m_Buffer.HistoricalData());
                bRectsChanged = true;
            }

            if (m_Buffer.RefreshModel())
            {
                m_ConceptMap.ClearFacts();

                foreach(var fact in m_Buffer.PlayerFacts())
                {
                    m_ConceptMap.AddFact(fact);
                }

                m_PlayerDisplay.LoadCritters(m_Buffer.PlayerData());
                bRectsChanged = true;
            }

            if (bRectsChanged)
            {
                Rect realRect = CalculateGraphRect(m_HistoricalDisplay.Range, m_PlayerDisplay.Range, m_Buffer.Scenario().TickCount(), 10);
                m_HistoricalDisplay.RenderLines(realRect);
                m_PlayerDisplay.RenderLines(realRect);
            }
        }

        static private Rect CalculateGraphRect(Rect inA, Rect inB, uint inTickCountX, uint inTickCountY)
        {
            float xMin = Math.Min(inA.xMin, inB.xMin), xMax = Math.Max(inA.xMax, inB.xMax),
                yMin = Math.Min(inA.yMin, inB.yMin), yMax = Math.Max(inA.yMax, inB.yMax);

            return GraphingUtils.CalculateAxisPair(Rect.MinMaxRect(xMin, yMin, xMax, yMax), inTickCountX, inTickCountY).ToRect();
        }
    }
}