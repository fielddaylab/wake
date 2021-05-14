using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Portable;
using BeauUtil;
using TMPro;
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

        [Header("Unadded Notification")]
        [SerializeField] private RectTransform m_UnaddedGroup = null;
        [SerializeField] private TMP_Text m_UnaddedLabel = null;
        
        #endregion // Inspector

        [NonSerialized] private UniversalModelState m_ModelState;

        public event Action<StringHash32> OnGraphUpdated;

        public void SetInitialFacts(IEnumerable<StringHash32> inFacts, UniversalModelState inModelState)
        {
            m_Map.ClearFacts();

            m_ModelState = inModelState;

            var bestiaryData = Services.Data.Profile.Bestiary;
            foreach(var factId in inFacts)
            {
                PlayerFactParams playerFact = bestiaryData.GetFact(factId);
                m_Map.AddFact(playerFact);
            }

            UpdateUnadded();
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
                m_ModelState.AddFact(inParams);
                OnGraphUpdated?.Invoke(inParams.FactId);
                UpdateUnadded();
            }
        }

        private void UpdateUnadded()
        {
            int count = m_ModelState.UngraphedFactCount();
            if (count > 0)
            {
                m_UnaddedGroup.gameObject.SetActive(true);
                m_UnaddedLabel.SetText(count.ToStringLookup());
            }
            else
            {
                m_UnaddedGroup.gameObject.SetActive(false);
            }
        }

        #endregion // Handlers
    }
}