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
    public class BestiaryApp : PortableMenuApp
    {
        #region Consts

        static private readonly StringHash32 Critters_Label = "ui.portable.app.bestiary.critters.label";
        static private readonly StringHash32 Ecosystems_Label = "ui.portable.app.bestiary.ecosystems.label";
        static private readonly StringHash32 Models_Label = "ui.portable.app.bestiary.models.label";

        static private readonly StringHash32 SelectCritter_Label = "ui.portable.app.bestiary.selectCritter.label";
        static private readonly StringHash32 SelectEcosystem_Label = "ui.portable.app.bestiary.selectEcosystem.label";

        static private readonly StringHash32 SelectCritterFact_Label = "ui.portable.app.bestiary.selectCritterFact.label";
        static private readonly StringHash32 SelectEcosystemFact_Label = "ui.portable.app.bestiary.selectEcosystemFact.label";
        static private readonly StringHash32 SelectModel_Label = "ui.portable.app.bestiary.selectModel.label";

        #endregion // Consts

        #region Types

        public class OpenToRequest : IPortableRequest
        {
            public BestiaryUpdateParams Target;

            public OpenToRequest(BestiaryUpdateParams inTarget)
            {
                Target = inTarget;
            }

            public PortableMenu.AppId AppId()
            {
                return PortableMenu.AppId.NULL; 
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return true;
            }

            public bool ForceInputEnabled()
            {
                return false;
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

            public PortableMenu.AppId AppId()
            {
                return Category == BestiaryDescCategory.Critter ? PortableMenu.AppId.Organisms : PortableMenu.AppId.Environments;
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return false;
            }

            public bool ForceInputEnabled()
            {
                return true;
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
            public Func<BFBase, bool> CustomValidator;
            public Future<StringHash32> Return;

            public SelectFactRequest(BestiaryDescCategory inCategory, Func<BFBase, bool> inCustomValidator = null)
            {
                Category = inCategory;
                CustomValidator = inCustomValidator;
                Return = Future.Create<StringHash32>();
            }

            public PortableMenu.AppId AppId()
            {
                return PortableMenu.AppId.Organisms;
                // return Category == BestiaryDescCategory.Critter ? PortableMenu.AppId.Organisms : PortableMenu.AppId.Environments;
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return false;
            }

            public bool ForceInputEnabled()
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
        [SerializeField] private PortableListElement.Pool m_EntryPool = null;
        [SerializeField] private LocText m_CategoryLabel = null;

        [Header("Group")]
        [SerializeField, Required] private RectTransform m_NoSelectionGroup = null;
        [SerializeField, Required] private LocText m_PromptText = null;

        [Header("Info")]
        [SerializeField, Required] private BestiaryPage m_CritterPage = null;
        [SerializeField, Required] private BestiaryPage m_EnvPage = null;
        [SerializeField, Required] private BestiaryPage m_ModelPage = null;

        [Header("Facts")]
        [SerializeField] private FactPools m_FactPools = null;

        #endregion // Inspector

        [NonSerialized] private PortableTweaks m_Tweaks = null;
        [NonSerialized] private BestiaryDescCategory m_CurrentEntryGroup = BestiaryDescCategory.Critter;
        [NonSerialized] private BestiaryDesc m_CurrentEntry;
        [NonSerialized] private BestiaryPage m_CurrentPage;
        
        [NonSerialized] private SelectBestiaryEntryRequest m_SelectBestiaryRequest = null;
        [NonSerialized] private SelectFactRequest m_SelectFactRequest = null;

        protected override void Awake()
        {
            base.Awake();

            m_CritterGroupToggle.onValueChanged.AddListener(OnCritterToggled);
            m_EcosystemGroupToggle.onValueChanged.AddListener(OnEcosystemToggled);
            m_ModelGroupToggle.onValueChanged.AddListener(OnModelToggled);

            RegisterPage(m_CritterPage);
            RegisterPage(m_EnvPage);
            RegisterPage(m_ModelPage);
        }

        private void RegisterPage(BestiaryPage inPage)
        {
            if (inPage.SelectButton)
            {
                inPage.SelectButton.onClick.AddListener(OnEntrySelectClicked);
                inPage.gameObject.SetActive(false);
            }
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

            Services.Events.Dispatch(GameEvents.PortableEntrySelected, (BestiaryDesc)inElement.Data);

            LoadEntry((BestiaryDesc) inElement.Data);
        }

        private void OnEntrySelectClicked()
        {
            Assert.NotNull(m_SelectBestiaryRequest);
            m_SelectBestiaryRequest.Return.Complete(m_CurrentEntry.Id());
            m_ParentMenu.Hide();
        }

        private void OnFactClicked(BFBase inFact)
        {
            Assert.NotNull(m_SelectFactRequest);
            m_SelectFactRequest.Return.Complete(inFact.Id);
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

            m_FactPools.FreeAll();
            m_NoSelectionGroup.gameObject.SetActive(true);

            m_EntryPool.Reset();
            m_EntryToggleGroup.SetAllTogglesOff(false);
            
            if (m_CurrentPage)
            {
                m_CurrentPage.gameObject.SetActive(false);
                m_CurrentPage = null;
            }
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
                        targetEntry = Assets.Bestiary(inTarget.Id);
                        break;
                    }

                case BestiaryUpdateParams.UpdateType.Unknown:
                    {
                        targetEntry = null;
                        break;
                    }

                default:
                    {
                        targetEntry = Assets.Fact(inTarget.Id).Parent;
                        break;
                    }
            }

            LoadEntryGroup(targetEntry == null ? BestiaryDescCategory.Critter : targetEntry.Category(), targetEntry, true);
        }

        private void LoadBestiarySelection(SelectBestiaryEntryRequest inSelect)
        {
            m_SelectBestiaryRequest = inSelect;

            m_PromptText.gameObject.SetActive(true);

            BestiaryDescCategory category = inSelect.Category;
            switch(inSelect.Category)
            {
                case BestiaryDescCategory.Critter:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText(SelectCritter_Label);
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText(SelectEcosystem_Label);
                        break;
                    }
                
                case BestiaryDescCategory.Model:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("-- Unsupported Mode --");
                        break;
                    }

                case BestiaryDescCategory.ALL:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("-- Unsupported Mode --");

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
                        m_PromptText.SetText(SelectCritterFact_Label);
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = false;
                        m_PromptText.SetText(SelectEcosystemFact_Label);
                        break;
                    }

                case BestiaryDescCategory.Model:
                    {
                        m_CritterGroupToggle.interactable = false;
                        m_EcosystemGroupToggle.interactable = false;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText(SelectModel_Label);
                        break;
                    }

                case BestiaryDescCategory.ALL:
                    {
                        m_CritterGroupToggle.interactable = true;
                        m_EcosystemGroupToggle.interactable = true;
                        m_ModelGroupToggle.interactable = true;
                        m_PromptText.SetText("-- Unsupported Mode --");

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

            if (m_CurrentPage)
                m_CurrentPage.gameObject.SetActive(false);

            switch(inType)
            {
                case BestiaryDescCategory.Critter:
                    m_CritterGroupToggle.SetIsOnWithoutNotify(true);
                    m_EcosystemGroupToggle.SetIsOnWithoutNotify(false);
                    m_ModelGroupToggle.SetIsOnWithoutNotify(false);

                    m_CategoryLabel.SetText(Critters_Label);
                    m_CurrentPage = m_CritterPage;
                    break;

                case BestiaryDescCategory.Environment:
                    m_EcosystemGroupToggle.SetIsOnWithoutNotify(true);
                    m_CritterGroupToggle.SetIsOnWithoutNotify(false);
                    m_ModelGroupToggle.SetIsOnWithoutNotify(false);

                    m_CategoryLabel.SetText(Ecosystems_Label);
                    m_CurrentPage = m_EnvPage;
                    break;

                case BestiaryDescCategory.Model:
                    m_ModelGroupToggle.SetIsOnWithoutNotify(true);
                    m_EcosystemGroupToggle.SetIsOnWithoutNotify(false);
                    m_CritterGroupToggle.SetIsOnWithoutNotify(false);

                    m_CategoryLabel.SetText(Models_Label);
                    m_CurrentPage = m_ModelPage;
                    break;
            }

            using(PooledList<BestiaryDesc> entities = PooledList<BestiaryDesc>.Create())
            {
                Services.Data.Profile.Bestiary.GetEntities(inType, entities);
                entities.Sort(BestiaryDesc.SortByEnvironment);
                foreach(var entry in entities)
                {
                    PortableListElement button = m_EntryPool.Alloc();
                    button.Initialize(entry.Icon(), m_EntryToggleGroup, entry.CommonName(), entry, OnEntryToggled);
                }
            }
            
            m_EntryLayoutGroup.ForceRebuild();
            LoadEntry(inTarget);

            // Services.Events.Dispatch(GameEvents.PortableBestiaryTabSelected, inType);
        }

        private void LoadEntry(BestiaryDesc inEntry)
        {
            m_FactPools.FreeAll();

            foreach(var button in m_EntryPool.ActiveObjects)
            {
                button.SetState((BestiaryDesc) button.Data == inEntry);
            }

            m_CurrentEntry = inEntry;

            if (inEntry == null)
            {
                m_CurrentPage.gameObject.SetActive(false);
                Services.Data?.SetVariable("portable:bestiary.currentEntry", null);
                m_NoSelectionGroup.gameObject.SetActive(true);
                return;
            }

            Services.Data.SetVariable("portable:bestiary.currentEntry", m_CurrentEntry.Id());

            m_NoSelectionGroup.gameObject.SetActive(false);
            m_CurrentPage.gameObject.SetActive(true);

            if (m_CurrentPage.ScientificName)
                m_CurrentPage.ScientificName.SetText(inEntry.ScientificName());

            if (m_CurrentPage.CommonName)
                m_CurrentPage.CommonName.SetText(inEntry.CommonName());

            if (m_CurrentPage.Sketch)
            {
                m_CurrentPage.Sketch.sprite = inEntry.Sketch();
                m_CurrentPage.Sketch.gameObject.SetActive(inEntry.Sketch());
            }

            if (m_CurrentPage.SelectButton)
            {
                if (m_SelectBestiaryRequest != null)
                {
                    m_CurrentPage.SelectButton.gameObject.SetActive(true);
                    m_CurrentPage.SelectButton.interactable = m_SelectBestiaryRequest.CustomValidator == null || m_SelectBestiaryRequest.CustomValidator(inEntry);
                }
                else
                {
                    m_CurrentPage.SelectButton.gameObject.SetActive(false);
                }
            }

            using(PooledList<BFBase> facts = PooledList<BFBase>.Create())
            {
                Services.Data.Profile.Bestiary.GetFactsForEntity(inEntry.Id(), facts);
                if (facts.Count > 0)
                {
                    if (m_CurrentPage.NoFacts)
                        m_CurrentPage.NoFacts.gameObject.SetActive(false);
                    if (m_CurrentPage.HasFacts)
                        m_CurrentPage.HasFacts.gameObject.SetActive(true);

                    facts.Sort(BFType.SortByVisualOrder);
                    foreach(var fact in facts)
                    {
                        VisitFact(fact);
                    }
                }
                else
                {
                    if (m_CurrentPage.HasFacts)
                        m_CurrentPage.HasFacts.gameObject.SetActive(false);
                    if (m_CurrentPage.NoFacts)
                        m_CurrentPage.NoFacts.gameObject.SetActive(true);
                }
            }

            m_CurrentPage.FactLayout.ForceRebuild();

            foreach(var layoutfix in m_CurrentPage.LayoutFixes)
                layoutfix.Rebuild();
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

        private void InstantiateFactButton(BFBase inFact) 
        {
            MonoBehaviour display = m_FactPools.Alloc(inFact, m_CurrentEntry, Services.Data.Profile.Bestiary.GetDiscoveredFlags(inFact.Id), m_CurrentPage.FactLayout.transform);
            BestiaryFactButton button = display.GetComponent<BestiaryFactButton>();
            if (m_SelectFactRequest != null)
            {
                button.Initialize(inFact, true, m_SelectFactRequest.CustomValidator == null || m_SelectFactRequest.CustomValidator(inFact), OnFactClicked);
            }
            else
            {
                button.Initialize(inFact, false, true, null);
            }
        }

        #region IFactVisitor

        private void VisitFact(BFBase inFact)
        {
            switch(inFact.Type)
            {
                case BFTypeId.Body:
                    break;

                default:
                    InstantiateFactButton(inFact);
                    break;
            }
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

        static public Future<StringHash32> RequestFact(BestiaryDescCategory inCategory, Func<BFBase, bool> inValidator = null)
        {
            var request = new SelectFactRequest(inCategory, inValidator);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            return request.Return;
        }

        #endregion // Static
    }
}