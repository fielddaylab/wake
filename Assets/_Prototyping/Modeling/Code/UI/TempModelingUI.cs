using System;
using Aqua;
using Aqua.Portable;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class TempModelingUI : MonoBehaviour
    {
        [Serializable] private class CritterSliderPool : SerializablePool<CritterPopulationSlider> { }

        #region Inspector

        [SerializeField] private Button m_AddButton = null;
        [SerializeField] private Button m_RemoveButton = null;
        [SerializeField] private CritterSliderPool m_SliderPool = null;

        #endregion // Inspector

        private SimulationBuffer m_Buffer;
        private Action m_OnUpdate;

        public void SetBuffer(SimulationBuffer inBuffer, Action inOnUpdate)
        {
            m_Buffer = inBuffer;
            m_OnUpdate = inOnUpdate;

            m_SliderPool.Reset();
            foreach(var critterType in inBuffer.Scenario().Critters())
            {
                var slider = m_SliderPool.Alloc();
                slider.Load(critterType, inBuffer.GetPlayerCritters(critterType.Id()));
                slider.OnPopulationChanged.AddListener(OnCritterPopulationChanged);
            }
        }

        #region Handlers

        private void Awake()
        {
            m_AddButton.onClick.AddListener(OnAddClicked);
            m_RemoveButton.onClick.AddListener(OnRemoveClicked);
        }

        private void OnAddClicked()
        {
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (p) => !m_Buffer.ContainsFact(p))
                .OnComplete((p) => {
                    if (m_Buffer.AddFact(p))
                    {
                        m_OnUpdate?.Invoke();
                    }
                });
        }

        private void OnRemoveClicked()
        {
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (p) => m_Buffer.ContainsFact(p))
                .OnComplete((p) => {
                    if (m_Buffer.RemoveFact(p))
                    {
                        m_OnUpdate?.Invoke();
                    }
                });
        }

        private void OnCritterPopulationChanged(ActorCount inActorCount)
        {
            m_Buffer.SetPlayerCritters(inActorCount.Id, inActorCount.Population);
            m_OnUpdate?.Invoke();
        }

        #endregion // Handlers
    }
}