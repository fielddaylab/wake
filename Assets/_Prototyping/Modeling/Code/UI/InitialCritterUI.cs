using System;
using Aqua;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class InitialCritterUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CritterPopulationSlider.Pool m_SliderPool = null;

        #endregion // Inspector

        private SimulationBuffer m_Buffer;
        
        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;
            
            m_SliderPool.Reset();
            var playerCritters = inBuffer.PlayerCritters();
            ModelingScenarioData scenario = inBuffer.Scenario();
            foreach(var critter in inBuffer.PlayerCritters())
            {
                if (!scenario.IsInHistorical(critter.Id()))
                    continue;
                
                StringHash32 id = critter.Id();
                var slider = m_SliderPool.Alloc();
                slider.Load(id, (int) inBuffer.GetPlayerCritters(id), 0, -1, (int) inBuffer.GetModelCritters(id));
                slider.OnPopulationChanged.AddListener(OnCritterPopulationChanged);
            }
        }

        #region Handlers

        private void OnCritterPopulationChanged(ActorCountI32 inActorCount)
        {
            m_Buffer.SetPlayerCritters(inActorCount.Id, (uint) inActorCount.Population);
        }

        #endregion // Handlers
    }
}