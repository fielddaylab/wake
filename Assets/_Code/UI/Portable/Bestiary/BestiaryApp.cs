using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua;
using BeauUtil.Debugger;

namespace Aqua.Portable
{
    public class BestiaryApp : PortableMenuApp, IFactVisitor
    {
        #region Types

        [Serializable] private class EntryPool : SerializablePool<PortableListElement> { }
        [Serializable] private class FactPool : SerializablePool<BestiaryFactButton> { }
        [Serializable] private class RangeFactPool : SerializablePool<BestiaryRangeFactButton> { }
        [Serializable] private class WaterPropertyPool : SerializablePool<BestiaryWaterPropertyButton> { }

        public class OpenToRequest : IPortableRequest
        {
            public BestiaryUpdateParams Target;

            public OpenToRequest(BestiaryUpdateParams inTarget)
            {
                Target = inTarget;
            }

            public StringHash32 AppId()
            {
                return "bestiary";
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return true;
            }

            public void Dispose()
            {
                Target = default(BestiaryUpdateParams);
            }
        }

        public class SelectBestiaryEntryRequest : IPortableRequest
        {
            public BestiaryDescCategory Category;
            public Func<BestiaryDesc, bool> CustomValidator;
            public Future<StringHash32> Return;

            public SelectBestiaryEntryRequest(BestiaryDescCategory inCategory, Func<BestiaryDesc, bool> inCustomValidator = null)
            {
                Category = inCategory;
                CustomValidator = inCustomValidator;
                Return = Future.Create<StringHash32>();
            }

            public StringHash32 AppId()
            {
                return "bestiary";
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return false;
            }

            public void Dispose()
            {
                CustomValidator = null;
                if (Return.IsInProgress())
                    Return.Fail();
                Ref.Dispose(ref Return);
            }
        }

        public class SelectFactRequest : IPortableRequest
        {
            public BestiaryDescCategory Category;
            public Func<PlayerFactParams, bool> CustomValidator;
            public Future<PlayerFactParams> Return;

            public SelectFactRequest(BestiaryDescCategory inCategory, Func<PlayerFactParams, bool> inCustomValidator = null)
            {
                Category = inCategory;
                CustomValidator = inCustomValidator;
                Return = Future.Create<PlayerFactParams>();
            }

            public StringHash32 AppId()
            {
                return "bestiary";
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return false;
            }

            public void Dispose()
            {
                CustomValidator = null;
                if (Return.IsInProgress())
                    Return.Fail();
                Ref.Dispose(ref Return);
            }
        }

        #endregion // Types

        #region Inspector

        [Header("Types")]
        [SerializeField, Required] private Toggle m_CritterGroupToggle = null;
        [SerializeField, Required] private Toggle m_EcosystemGroupToggle = null;
        [SerializeField, Required] private Toggle m_ModelGroupToggle = null;

        [Header("Entries")]
        [SerializeField, Required] private VerticalLayoutGroup m_EntryLayoutGroup = null;
        [SerializeField, Required] private ToggleGroup m_EntryToggleGroup = null;
        [SerializeField] private EntryPool m_EntryPool = null;

        [Header("Group")]
        [SerializeField, Required] private RectTransform m_NoSelectionGroup = null;
        [SerializeField, Required] private RectTransform m_HasSelectionGroup = null;
        [SerializeField, Required] private LocText m_PromptText = null;

        [Header("Info")]
        [SerializeField, Required] private LocText m_ScientificNameLabel = null;
        [SerializeField, Required] private LocText m_CommonNameLabel = null;
        [SerializeField, Required] private Image m_SketchImage = null;
        [SerializeField, Required] private VerticalLayoutGroup m_FactLayoutGroup = null;
        [SerializeField, Required] private Button m_SelectEntryButton = null;

        [Header("Facts")]
        [SerializeField] private FactPool m_FactPool = null;
        [SerializeField] private RangeFactPool m_RangeFactPool = null;
        [SerializeField] private WaterPropertyPool m_WaterPropertyPool = null;

        #endregion // Inspector

        [NonSerialized] private PortableMenu m_ParentMenu = null;
        [NonSerialized] private PortableTweaks m_Tweaks = null;
        [NonSerialized] private BestiaryDescCategory m_CurrentEntryGroup = BestiaryDescCategory.Critter;
        [NonSerialized] private BestiaryDesc m_CurrentEntry;
        
        [NonSerialized] private SelectBestiaryEntryRequest m_SelectBestiaryRequest = null;
        [NonSerialized] private SelectFactRequest m_SelectFactRequest = null;

        protected override void Awake()
        {
            base.Awake();

            m_CritterGroupToggle.onValueChanged.AddListener(OnCritterToggled);
            m_EcosystemGroupToggle.onValueChanged.AddListener(OnEcosystemToggled);
            m_ModelGroupToggle.onValueChanged.AddListener(OnModelToggled);

            m_SelectEntryButton.onClick.AddListener(OnEntrySelectClicked);
            m_SelectEntryButton.gameObject.SetActive(false);

            m_ParentMenu = GetComponentInParent<PortableMenu>();
        }

        #region Callbacks

        private void OnCritterToggled(bool inbOn)
        {
            if (!IsShowing())
                return;

            if (inbOn)
                LoadEntryGroup(BestiaryDescCategory.Critter, null, false);
        }

        private void OnModelToggled(bool inbOn)
        {
            if (!IsShowing())
                return;

            if (inbOn)
                LoadEntryGroup(BestiaryDescCategory.Model, null, false);
        }

        private void OnEcosystemToggled(bool inbOn)
        {
            if (!IsShowing())
                return;
                
            if (inbOn)
                LoadEntryGroup(BestiaryDescCategory.Environment, null, false);
        }

        private void OnEntryToggled(PortableListElement inElement, bool inbOn)
        {
            if (!inbOn)
            {
                if (!m_EntryToggleGroup.AnyTogglesOn())
                    LoadEntry(null);
                return;
            }

            LoadEntry((BestiaryDesc) inElement.Data);
        }

        // private void OnModelEntryToggled(PortableListElement inElement, bool inbOn)
        // {
        //     if (!inbOn)
        //     {
        //         if (!m_EntryToggleGroup.AnyTogglesOn())
        //             LoadModelEntry(null);
        //         return;
        //     }

        //     LoadModelEntry((Artifact) inElement.Data);
        // }

        private void OnEntrySelectClicked()
        {
            Assert.NotNull(m_SelectBestiaryRequest);
            m_SelectBestiaryRequest.Return.Complete(m_CurrentEntry.Id());
            m_ParentMenu.Hide();
        }

        private void OnFactClicked(PlayerFactParams inFact)
        {
            Assert.NotNull(m_SelectFactRequest);
            m_SelectFactRequest.Return.Complete(inFact);
            m_ParentMenu.Hide();
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            m_Tweaks = Services.Tweaks.Get<PortableTweaks>();

            m_CritterGroupToggle.SetIsOnWithoutNotify(true);
            m_EcosystemGroupToggle.SetIsOnWithoutNotify(false);
            m_ModelGroupToggle.SetIsOnWithoutNotify(false);
            LoadEntryGroup(BestiaryDescCategory.Critter, null, true);

            m_CritterGroupToggle.interactable = true;
            m_EcosystemGroupToggle.interactable = true;
            m_ModelGroupToggle.interactable = true;

            LoadEntry(null);
        }

        protected override void OnHide(bool inbInstant)
        {
            Services.Data?.SetVariable("portable:bestiary.currentEntry", null);

            m_FactPool.Reset();
            m_RangeFactPool.Reset();
            m_WaterPropertyPool.Reset();
            m_NoSelectionGroup.gameObject.SetActive(true);
            m_HasSelectionGroup.gameObject.SetActive(false);

            m_EntryPool.Reset();
            m_EntryToggleGroup.SetAllTogglesOff(false);
            
            m_SelectEntryButton.gameObject.SetActive(false);
            m_PromptText.gameObject.SetActive(false);

            Ref.Dispose(ref m_SelectBestiaryRequest);
            Ref.Dispose(ref m_SelectFactRequest);

            m_CurrentEntry = null;

            base.OnHide(inbInstant);
        }

        #endregion // Callbacks

        #region Loading

        private void LoadTarget(BestiaryUpdateParams inTarget)
        {
            BestiaryDesc targetEntry;
            switch(inTarget.Type)
            {
                case BestiaryUpdateParams.UpdateType.Entity:
                    {
                        targetEntry = Services.Assets.Bestiary.Get(inTarget.Id);
                        break;
                    }

                default:
                    {
                        targetEntry = Services.Assets.Bestiary.Fact(inTarget.Id).Parent();
                        break;
                    }
            }

            LoadEntryGroup(targetEntry.Category(), targetEntry, true);
        }

        private void LoadBestiarySelection(SelectBestiaryEntryRequest inSelect)
        {
            m_SelectBestiaryRequest = inSelect;

            m_PromptText.gameObject.SetActive(true);
            m_SelectEntryButton.gameObject.SetActive(true);

            BestiaryDescCategory category = inSelect.Category;
            switch(inSelect.Category)
            {
                case BestiaryDescCategory.Critter:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText("Select Critter");
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText("Select Environment");
                        break;
                    }
                
                case BestiaryDescCategory.Model:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("Select Model");
                        break;
                    }

                case BestiaryDescCategory.ALL:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("Select Entry");

                        category = BestiaryDescCategory.Critter;
                        break;
                    }
            }

            LoadEntryGroup(category, null, true);
        }

        private void LoadFactSelection(SelectFactRequest inSelect)
        {
            m_SelectFactRequest = inSelect;

            m_PromptText.gameObject.SetActive(true);

            BestiaryDescCategory category = inSelect.Category;
            switch(inSelect.Category)
            {
                case BestiaryDescCategory.Critter:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText("Select Critter Fact");
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText("Select Environment Variable");
                        break;
                    }

                case BestiaryDescCategory.Model:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("Select Model");
                        break;
                    }

                case BestiaryDescCategory.ALL:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("Select Fact");

                        category = BestiaryDescCategory.Critter;
                        break;
                    }
            }

            LoadEntryGroup(category, null, true);
        }

        private void LoadEntryGroup(BestiaryDescCategory inType, BestiaryDesc inTarget, bool inbForce)
        {
            if (!inbForce && m_CurrentEntryGroup == inType)
                return;

            m_CurrentEntryGroup = inType;
            m_EntryPool.Reset();

            switch(inType)
            {
                case BestiaryDescCategory.Critter:
                    m_CritterGroupToggle.SetIsOnWithoutNotify(true);
                    break;

                case BestiaryDescCategory.Environment:
                    m_EcosystemGroupToggle.SetIsOnWithoutNotify(true);
                    break;

                case BestiaryDescCategory.Model:
                    m_ModelGroupToggle.SetIsOnWithoutNotify(true);
                    break;
            }

            Color buttonColor = m_Tweaks.BestiaryListColor(inType);

            foreach(var entry in Services.Data.Profile.Bestiary.GetEntities(inType))
            {
                PortableListElement button = m_EntryPool.Alloc();
                button.Initialize(entry.Icon(), buttonColor, m_EntryToggleGroup, entry.CommonName(), entry, OnEntryToggled);
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_EntryLayoutGroup.transform);
            LoadEntry(inTarget);
        }

        private void LoadEntry(BestiaryDesc inEntry)
        {
            m_FactPool.Reset();
            m_RangeFactPool.Reset();
            m_WaterPropertyPool.Reset();

            foreach(var button in m_EntryPool.ActiveObjects)
            {   
                button.SetState((BestiaryDesc) button.Data == inEntry);
            }

            m_CurrentEntry = inEntry;

            if (inEntry == null)
            {
                Services.Data.SetVariable("portable:bestiary.currentEntry", null);
                m_NoSelectionGroup.gameObject.SetActive(true);
                m_HasSelectionGroup.gameObject.SetActive(false);
                return;
            }

            Services.Data?.SetVariable("portable:bestiary.currentEntry", m_CurrentEntry.Id());

            m_NoSelectionGroup.gameObject.SetActive(false);
            m_HasSelectionGroup.gameObject.SetActive(true);

            m_ScientificNameLabel.SetText(inEntry.ScientificName());
            m_CommonNameLabel.SetText(inEntry.CommonName());

            m_SketchImage.sprite = inEntry.Sketch();
            m_SketchImage.gameObject.SetActive(inEntry.Sketch());

            if (m_SelectBestiaryRequest != null)
            {
                m_SelectEntryButton.interactable = m_SelectBestiaryRequest.CustomValidator == null || m_SelectBestiaryRequest.CustomValidator(inEntry);
            }

            using(PooledList<PlayerFactParams> facts = PooledList<PlayerFactParams>.Create())
            {
                Services.Data.Profile.Bestiary.GetFactsForEntity(inEntry.Id(), facts);
                facts.Sort();
                foreach(var fact in facts)
                {
                    fact.Fact.Accept(this, fact);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_FactLayoutGroup.transform);
        }

        #endregion // Loading

        public override bool TryHandle(IPortableRequest inRequest)
        {
            OpenToRequest openTo = inRequest as OpenToRequest;
            if (openTo != null)
            {
                Show();
                LoadTarget(openTo.Target);
                return true;
            }

            SelectBestiaryEntryRequest bestiarySelect = inRequest as SelectBestiaryEntryRequest;
            if (bestiarySelect != null)
            {
                Show();
                LoadBestiarySelection(bestiarySelect);
                return true;
            }

            SelectFactRequest factSelect = inRequest as SelectFactRequest;
            if (factSelect != null)
            {
                Show();
                LoadFactSelection(factSelect);
                return true;
            }
            
            return false;
        }

        private void InstantiateFactButton(BFBase inFact, PlayerFactParams inParams) 
        {
            BestiaryFactButton factButton = m_FactPool.Alloc();
            if (m_SelectFactRequest != null)
            {
                factButton.Initialize(inFact, inParams, true, m_SelectFactRequest.CustomValidator == null || m_SelectFactRequest.CustomValidator(inParams), OnFactClicked);
            }
            else
            {
                factButton.Initialize(inFact, inParams, false, true, null);
            }
        }

        private void InstantiateRangeFactButton(BFStateRange inFact, PlayerFactParams inParams) 
        {
            BestiaryRangeFactButton factButton = m_RangeFactPool.Alloc();
            if (m_SelectFactRequest != null)
            {
                factButton.Initialize(inFact, inParams, true, m_SelectFactRequest.CustomValidator == null || m_SelectFactRequest.CustomValidator(inParams), OnFactClicked);
            }
            else
            {
                factButton.Initialize(inFact, inParams, false, true, null);
            }
        }

        #region IFactVisitor

        void IFactVisitor.Visit(BFBase inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFBody inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFWaterProperty inFact, PlayerFactParams inParams)
        {
            BestiaryWaterPropertyButton factButton = m_WaterPropertyPool.Alloc();
            if (m_SelectFactRequest != null)
            {
                factButton.Initialize(inFact, inParams, true, m_SelectFactRequest.CustomValidator == null || m_SelectFactRequest.CustomValidator(inParams), OnFactClicked);
            }
            else
            {
                factButton.Initialize(inFact, inParams, false, true, null);
            }
        }

        void IFactVisitor.Visit(BFEat inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFGrow inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFReproduce inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFStateStarvation inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFStateRange inFact, PlayerFactParams inParams)
        {
            InstantiateRangeFactButton(inFact, inParams);
        }

        void IFactVisitor.Visit(BFStateAge inFact, PlayerFactParams inParams)
        {
            InstantiateFactButton(inFact, inParams);
        }
    
        #endregion // IFactVisitor
    
        #region Static

        static public void OpenToEntry(StringHash32 inId)
        {
            var request = new OpenToRequest(new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Entity, inId));
            Services.UI.FindPanel<PortableMenu>().Open(request);
        }

        static public void OpenToFact(StringHash32 inId)
        {
            var request = new OpenToRequest(new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Fact, inId));
            Services.UI.FindPanel<PortableMenu>().Open(request);
        }

        static public Future<StringHash32> RequestEntity(BestiaryDescCategory inCategory, Func<BestiaryDesc, bool> inValidator = null)
        {
            var request = new SelectBestiaryEntryRequest(inCategory, inValidator);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            return request.Return;
        }

        static public Future<PlayerFactParams> RequestFact(BestiaryDescCategory inCategory, Func<PlayerFactParams, bool> inValidator = null)
        {
            var request = new SelectFactRequest(inCategory, inValidator);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            return request.Return;
        }

        #endregion // Static
    }
}