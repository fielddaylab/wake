using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Portable;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ConceptMapUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private ConceptMap m_Map = null;
        [SerializeField] private CanvasGroup m_CritterSelectGroup = null;
        [SerializeField] private Button m_AddButton = null;
        
        #endregion // Inspector

        [NonSerialized] private HashSet<StringHash32> m_GraphedCritters = new HashSet<StringHash32>();

        public event Action<StringHash32> OnGraphUpdated;

        public void SetInitialFacts(ListSlice<StringHash32> inFacts)
        {
            m_Map.ClearFacts();

            var bestiaryData = Services.Data.Profile.Bestiary;
            foreach(var factId in inFacts)
            {
                PlayerFactParams playerFact = bestiaryData.GetFact(factId);
                m_Map.AddFact(playerFact);
                playerFact.Fact.CollectReferences(m_GraphedCritters);
            }
        }

        public bool IsGraphed(StringHash32 inCritterId)
        {
            return m_GraphedCritters.Contains(inCritterId);
        }

        #region Handlers

        private void Awake()
        {
            m_AddButton.onClick.AddListener(OnAddClicked);
            m_CritterSelectGroup.blocksRaycasts = false;
        }

        private void OnAddClicked()
        {
            var bestiaryData = Services.Data.Profile.Bestiary;
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (p) => !bestiaryData.IsFactGraphed(p.FactId))
                .OnComplete(Add);
        }

        private void Add(PlayerFactParams inParams)
        {
            if (Services.Data.Profile.Bestiary.AddFactToGraph(inParams.FactId))
            {
                m_Map.AddFact(inParams);
                inParams.Fact.CollectReferences(m_GraphedCritters);
                OnGraphUpdated?.Invoke(inParams.FactId);
            }
        }

        #endregion // Handlers
    }
}