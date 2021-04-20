using System;
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
        [SerializeField] private CanvasGroup m_CritterSelectGroup = null;
        [SerializeField] private Button m_AddButton = null;
        
        #endregion // Inspector

        [NonSerialized] private SimulationBuffer m_Buffer = null;
        [NonSerialized] private HashSet<PlayerFactParams> m_Facts = new HashSet<PlayerFactParams>();
        [NonSerialized] private HashSet<BestiaryDesc> m_SelectedCritters = new HashSet<BestiaryDesc>();
        [NonSerialized] private bool m_AllowHighlight = false;

        private SelectionFactFilter m_SelectionFilter;

        public void SetInitialFacts(ListSlice<StringHash32> inFacts)
        {
            m_Map.ClearFacts();

            m_Facts.Clear();
            var bestiaryData = Services.Data.Profile.Bestiary;
            foreach(var factId in inFacts)
            {
                PlayerFactParams playerFact = bestiaryData.GetFact(factId);
                m_Facts.Add(playerFact);
                m_Map.AddFact(playerFact);
            }
        }

        public void SetBuffer(SimulationBuffer inBuffer)
        {
            m_Buffer = inBuffer;
            m_SelectionFilter.Buffer = inBuffer;
        }

        public void SetHighlightAllowed(bool inbAllowed)
        {
            if (m_AllowHighlight == inbAllowed)
                return;
            
            m_AllowHighlight = inbAllowed;
            m_CritterSelectGroup.blocksRaycasts = inbAllowed;

            if (!inbAllowed)
            {
                m_SelectedCritters.Clear();

                foreach(var node in m_Map.Nodes())
                {
                    node.SetGlowing(false);
                }
            }
            else
            {
                m_Buffer.ClearFacts();
                m_Buffer.ClearSelectedCritters();
            }
        }

        #region Handlers

        private void Awake()
        {
            m_AddButton.onClick.AddListener(OnAddClicked);
            m_Map.OnNodeRequestToggle = OnNodeRequestToggle;

            m_SelectionFilter = new SelectionFactFilter();
            m_SelectionFilter.Selected = m_SelectedCritters;

            m_CritterSelectGroup.blocksRaycasts = false;
        }

        private void OnAddClicked()
        {
            BestiaryApp.RequestFact(BestiaryDescCategory.Critter, (p) => !m_Facts.Contains(p))
                .OnComplete(Add);
        }

        private void Add(PlayerFactParams inParams)
        {
            if (Services.Data.Profile.Bestiary.AddFactToGraph(inParams.FactId))
            {
                m_Facts.Add(inParams);
                m_Map.AddFact(inParams);
            }
        }

        private void OnNodeRequestToggle(BestiaryDesc inDesc)
        {
            if (!m_AllowHighlight)
                return;
            
            if (m_SelectedCritters.Add(inDesc))
            {
                m_Map.Node(inDesc.Id()).SetGlowing(true);
            }
            else
            {
                m_SelectedCritters.Remove(inDesc);
                m_Map.Node(inDesc.Id()).SetGlowing(false);
            }

            UpdateBufferFromSelections();
        }

        #endregion // Handlers

        #region Updating

        private void UpdateBufferFromSelections()
        {
            m_Buffer.ClearFacts();
            m_Buffer.ClearSelectedCritters();

            foreach(var critter in m_SelectedCritters)
            {
                m_Buffer.SelectCritter(critter);
            }
        
            foreach(var fact in m_Facts)
            {
                fact.Fact.Accept(m_SelectionFilter, fact);
            }
        }

        private class SelectionFactFilter : IFactVisitor
        {
            public HashSet<BestiaryDesc> Selected;
            public SimulationBuffer Buffer;

            public void Visit(BFBase inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFBody inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFWaterProperty inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFEat inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()) && Selected.Contains(inFact.Target()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFGrow inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFReproduce inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFConsume inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFProduce inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFStateStarvation inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFStateRange inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }

            public void Visit(BFStateAge inFact, PlayerFactParams inParams = null)
            {
                if (Selected.Contains(inFact.Parent()))
                    Buffer.AddFact(inParams);
            }
        }

        #endregion // Updating
    }
}