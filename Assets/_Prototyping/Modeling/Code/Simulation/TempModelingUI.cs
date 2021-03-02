using System;
using Aqua;
using Aqua.Portable;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class TempModelingUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_AddButton = null;
        [SerializeField] private Button m_RemoveButton = null;

        #endregion // Inspector

        private SimulationBuffer m_Buffer;
        private Action m_OnUpdate;

        public void SetBuffer(SimulationBuffer inBuffer, Action inOnUpdate)
        {
            m_Buffer = inBuffer;
            m_OnUpdate = inOnUpdate;
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

        #endregion // Handlers
    }
}