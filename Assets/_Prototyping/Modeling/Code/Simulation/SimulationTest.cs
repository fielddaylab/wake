using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class SimulationTest : MonoBehaviour, ISceneLoadHandler
    {
        [SerializeField] private ModelingScenarioData m_TestScenario = null;
        [SerializeField] private GraphDisplay m_Display = null;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            SimulationBuffer buffer = new SimulationBuffer();
            buffer.SetScenario(m_TestScenario);
            // buffer.Flags = SimulatorFlags.Debug;
            buffer.RefreshHistorical();

            var displayRange = m_Display.InitializeCritters(buffer.HistoricalData());
            var rangeX = GraphingUtils.CalculateAxis(displayRange.xMin, displayRange.xMax, (uint) m_TestScenario.TickCount());
            var rangeY = GraphingUtils.CalculateAxis(displayRange.yMin, displayRange.yMax, 10);
            displayRange.Set(rangeX.Min, rangeY.Min, rangeX.Max - rangeX.Min, rangeY.Max - rangeY.Min);
            m_Display.RenderLines(displayRange);
        }
    }
}