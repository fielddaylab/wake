using System.Collections.Generic;
using Aqua;
using Aqua.Portable;
using BeauRoutine.Extensions;
using BeauUtil;
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

        private List<PlayerFactParams> m_FactIs = new List<PlayerFactParams>();

        public void SetInitialFacts(ListSlice<StringHash32> inFacts)
        {
            m_Map.ClearFacts();

            m_FactIs.Clear();
            var bestiaryData = Services.Data.Profile.Bestiary;
            foreach(var factId in inFacts)
            {
                PlayerFactParams playerFact = bestiaryData.GetFact(factId);
                m_FactIs.Add(playerFact);
                m_Map.AddFact(playerFact);
            }
        }

        #region Handlers

        private void Awake()
        {
            m_AddButton.onClick.AddListener(OnAddClicked);
            // m_Map.OnLinkRequestRemove = Remove;
        }

        private void OnAddClicked()
        {
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (p) => !m_FactIs.Contains(p))
                .OnComplete(Add);
        }

        private void Add(PlayerFactParams inParams)
        {
            if (Services.Data.Profile.Bestiary.AddFactToGraph(inParams.FactId))
            {
                m_FactIs.Add(inParams);
                m_Map.AddFact(inParams);
            }
        }

        #endregion // Handlers
    }
}