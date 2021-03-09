using Aqua;
using Aqua.Portable;
using BeauRoutine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ConceptMapUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private ConceptMap m_Map = null;
        [SerializeField] private Button m_AddButton = null;
        
        #endregion // Inspector

        private SimulationBuffer m_Buffer;

        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;
        }

        public void Lock()
        {
            m_Map.Lock();
            m_AddButton.interactable = false;
        }

        #region Handlers

        private void Awake()
        {
            m_AddButton.onClick.AddListener(OnAddClicked);
            m_Map.OnLinkRequestRemove = Remove;
        }

        private void OnAddClicked()
        {
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (p) => !m_Buffer.ContainsFact(p))
                .OnComplete(Add);
        }

        private void Add(PlayerFactParams inParams)
        {
            m_Buffer.AddFact(inParams);
            m_Map.AddFact(inParams);
        }

        private void Remove(PlayerFactParams inParams)
        {
            m_Buffer.RemoveFact(inParams);
            m_Map.RemoveFact(inParams);
        }

        #endregion // Handlers
    }
}