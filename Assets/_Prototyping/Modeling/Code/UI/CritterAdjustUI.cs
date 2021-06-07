using System;
using Aqua;
using Aqua.Portable;
using BeauPools;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class CritterAdjustUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CritterPopulationSlider.Pool m_SliderPool = null;

        #endregion // Inspector

        private SimulationBuffer m_Buffer;
        
        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;
            
            m_SliderPool.Reset();
            var historicalBuffer = inBuffer.HistoricalEndState();
            foreach(var critterType in inBuffer.Scenario().AdjustableActors())
            {
                var slider = m_SliderPool.Alloc();
                slider.Load(critterType.Id, 0, -(int) historicalBuffer.GetCritters(critterType.Id).Population, inBuffer.GetPlayerPredictionCritterAdjust(critterType.Id));
                slider.OnPopulationChanged.AddListener(OnCritterPopulationChanged); 
            }
        }

        #region Handlers

        private void OnCritterPopulationChanged(ActorCountI32 inActorCount)
        {
            m_Buffer.SetPlayerPredictionCritterAdjust(inActorCount.Id, inActorCount.Population);
        }

        #endregion // Handlers
    }
}