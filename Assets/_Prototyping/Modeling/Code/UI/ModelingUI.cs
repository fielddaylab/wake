using System;
using Aqua;
using BeauRoutine.Extensions;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class ModelingUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private ConceptMapUI m_ConceptMap = null;
        [SerializeField] private ChartUI m_Chart = null;
        [SerializeField] private InitialCritterUI m_InitialCritters = null;
        [SerializeField] private CritterAdjustUI m_CritterAdjust = null;

        #endregion // Inspector

        private SimulationBuffer m_Buffer;
        
        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;

            m_ConceptMap.SetBuffer(inBuffer);
            m_InitialCritters.SetBuffer(inBuffer);
            m_CritterAdjust.SetBuffer(inBuffer);
        }

        public void Refresh()
        {
            bool bUpdated = m_Chart.Refresh(m_Buffer);

            if (bUpdated)
            {
                float error = m_Buffer.CalculateModelError();
                float sync = 100 - error * 100;
                // m_UI.DisplaySync(sync);
            }
        }
    }
}