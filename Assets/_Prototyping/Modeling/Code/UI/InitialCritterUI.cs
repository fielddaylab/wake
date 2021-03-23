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
            foreach(var critterPair in inBuffer.Scenario().Actors())
            {
                var slider = m_SliderPool.Alloc();
                slider.Load(critterPair.Id, (int) inBuffer.GetPlayerCritters(critterPair.Id), -1, (int) inBuffer.GetModelCritters(critterPair.Id));
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