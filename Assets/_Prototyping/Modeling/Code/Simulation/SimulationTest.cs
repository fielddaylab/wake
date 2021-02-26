using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class SimulationTest : MonoBehaviour, ISceneLoadHandler
    {
        [SerializeField] private ModelingScenarioData m_TestScenario = null;

        private void LogSimulationResult(in SimulationResult inResult)
        {
            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                psb.Builder.Append("[SimulationTest] Current values")
                    .Append("\n Oxygen = ").Append(inResult.Environment.Oxygen)
                    .Append("\n Temperature = ").Append(inResult.Environment.Temperature)
                    .Append("\n Light = ").Append(inResult.Environment.Light)
                    .Append("\n PH = ").Append(inResult.Environment.PH)
                    .Append("\n CarbonDioxide = ").Append(inResult.Environment.CarbonDioxide)
                    .Append("\n Salinity = ").Append(inResult.Environment.Salinity);
                for(int i = 0; i < inResult.Actors.Count; ++i)
                {
                    var actorStats = inResult.Actors[i];
                    psb.Builder.Append("\n ").Append(actorStats.Id.ToDebugString())
                        .Append(" Population = ").Append(actorStats.Population)
                        .Append(" State = ").Append(actorStats.State);
                }
                Debug.Log(psb.ToString());
            }
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            SimulationProfile profile = new SimulationProfile();
            profile.Construct(m_TestScenario.Environment(), m_TestScenario.Facts());
            foreach(var actor in m_TestScenario.Actors())
            {
                profile.InitialState.SetCritters(actor.Id, actor.Population);
            }

            SimulationResult current = profile.InitialState;
            LogSimulationResult(current);
            for(int i = 0; i < m_TestScenario.TickCount(); ++i)
            {
                current = Simulator.Simulate(profile, current);
                LogSimulationResult(current);
            }
        }
    }
}