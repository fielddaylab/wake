using System;
using Aqua;
using BeauRoutine.Extensions;
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
            foreach(var critterType in inBuffer.Scenario().Critters())
            {
                var slider = m_SliderPool.Alloc();
                slider.Load(critterType, (int) inBuffer.GetPlayerCritters(critterType.Id()));
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