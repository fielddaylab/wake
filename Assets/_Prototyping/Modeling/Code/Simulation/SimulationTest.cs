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

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            SimulationBuffer buffer = new SimulationBuffer();
            buffer.SetScenario(m_TestScenario);
            buffer.Flags = SimulatorFlags.Debug;
            buffer.RefreshHistorical();
        }
    }
}