using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable {
    public sealed class BestiaryApp : PortableMenuApp {

        static private readonly NamedOption[] PresentFactOptions = new NamedOption[] {
            new NamedOption("present", "ui.popup.presentButton")
        };

        public delegate void PopulateEntryToggleDelegate(PortableBestiaryToggle inToggle, BestiaryDesc inEntry);
        public delegate void PopulateEntryPageDelegate(BestiaryPage inPage, BestiaryDesc inEntry);
        public delegate void PopulateFactsDelegate(BestiaryPage inPage, BestiaryDesc inEntry, ListSlice<BFBase> inFacts, FinalizeButtonDelegate inFinalizeCallback);
        public delegate void FinalizeButtonDelegate(BFBase inFact, MonoBehaviour inDisplay);

        public delegate bool CanSelectFactDelegate(BFBase inFact);
        public delegate bool SelectForSetDelegate(BFBase inFact, Future<StringHash32> ioFuture);

        public class DisplayHandler {
            public BestiaryDescCategory Category;
            public PopulateEntryToggleDelegate PopulateToggle;
            public PopulateEntryPageDelegate PopulatePage;
            public PopulateFactsDelegate PopulateFacts;
        }

        #region Inspector

        [Header("Entries")]
        [SerializeField, Required] private ScrollRect m_EntryScroll = null;
        [SerializeField, Required] private VerticalLayoutGroup m_EntryLayoutGroup = null;
        [SerializeField, Required] private ToggleGroup m_EntryToggleGroup = null;
        [SerializeField, Required] private BestiaryPools m_ListPools = null;
        [SerializeField, Required] private RectTransform m_ListObjectRoot = null;

        [Header("Group")]
        [SerializeField, Required] private GameObject m_NoSelectionGroup = null;

        [Header("Info")]
        [SerializeField, Required] private BestiaryPage m_InfoPage = null;

        #endregion // Inspector

        public DisplayHandler Handler;

        [NonSerialized] private BestiaryDesc m_CurrentEntry;
        [NonSerialized] private PortableBestiaryToggle.ToggleDelegate m_CachedToggleDelegate;
        [NonSerialized] private List<BestiaryFactButton> m_InstantiatedButtons = new List<BestiaryFactButton>();
        [NonSerialized] private Action<BFBase> m_CachedSelectFactDelegate;
        [NonSerialized] private PortableRequest m_Request;
        [NonSerialized] private int m_SelectCounter;

        #region Panel

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShowComplete(inbInstant);

            LoadEntries();
            LoadEntry(null, false);
        }

        protected override void OnHide(bool inbInstant) {
            Services.Data?.SetVariable("portable:bestiary.currentEntry", null);

            m_InfoPage.Sketch.Clear();
            m_InfoPage.FactPools.FreeAll();
            m_InstantiatedButtons.Clear();
            m_NoSelectionGroup.gameObject.SetActive(true);

            m_ListPools.Clear();
            m_EntryToggleGroup.SetAllTogglesOff(false);

            m_InfoPage.gameObject.SetActive(false);

            m_CurrentEntry = null;
            m_SelectCounter = 0;

            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Callbacks

        private void OnEntryToggled(PortableBestiaryToggle inElement, bool inbOn) {
            if (!inbOn) {
                if (!m_EntryToggleGroup.AnyTogglesOn())
                    LoadEntry(null, false);
                return;
            }

            Services.Events.Dispatch(GameEvents.PortableEntrySelected, (BestiaryDesc)inElement.Data);

            LoadEntry((BestiaryDesc)inElement.Data, false);
        }

        private void OnFactClicked(BFBase inFact) {
            Services.Data.SetVariable("portable:lastSelectedFactId", inFact.Id);
            NamedOption[] options;
            if (m_Request.Type == PortableRequestType.SelectFact || m_Request.Type == PortableRequestType.SelectFactSet) {
                options = PresentFactOptions;
            } else {
                options = Array.Empty<NamedOption>();
            }

            var request = Script.PopupFactDetails(inFact, Save.Bestiary.GetDiscoveredFlags(inFact.Id), options);
            request.OnComplete((o) => {
                if (!o.IsEmpty) {
                    Assert.True(m_Request.Type == PortableRequestType.SelectFact || m_Request.Type == PortableRequestType.SelectFactSet);
                    if (m_Request.OnSelect != null) {
                        if (!m_Request.OnSelect(inFact, m_Request.Response)) {
                            m_ParentMenu.Hide();
                        } else {
                            UpdateAllFactInstances();
                        }
                    } else {
                        m_Request.Response.Complete(inFact.Id);
                        m_ParentMenu.Hide();
                    }
                }
            });
        }

        #endregion // Callbacks

        #region Loading

        /// <summary>
        /// Loads all organism entries.
        /// </summary>
        private void LoadEntries() {
            m_ListPools.Clear();

            using(PooledList<BestiaryDesc> entities = PooledList<BestiaryDesc>.Create()) {
                Save.Bestiary.GetEntities(Handler.Category, entities);
                entities.Sort(BestiaryDesc.SortByEnvironment);
                StringHash32 mapId = default;

                m_ListPools.PrewarmEntries(entities.Count);

                foreach (var entry in entities) {
                    if (mapId != entry.StationId()) {
                        mapId = entry.StationId();
                        PortableStationHeader header = m_ListPools.AllocHeader(m_ListObjectRoot);
                        MapDesc map = Assets.Map(mapId);
                        header.Header.SetText(map.StationHeaderId());
                        header.SubHeader.SetText(map.ShortLabelId());
                    }

                    PortableBestiaryToggle toggle = m_ListPools.AllocEntry(m_ListObjectRoot);
                    toggle.Toggle.group = m_EntryToggleGroup;
                    toggle.Toggle.SetIsOnWithoutNotify(false);
                    toggle.Data = entry;
                    toggle.Callback = m_CachedToggleDelegate ?? (m_CachedToggleDelegate = OnEntryToggled);
                    Handler.PopulateToggle(toggle, entry);
                }
            }

            m_EntryLayoutGroup.ForceRebuild();
        }

        /// <summary>
        /// Loads an organism entry.
        /// </summary>
        private void LoadEntry(BestiaryDesc inEntry, bool inbSyncToggles) {
            m_CurrentEntry = inEntry;

            if (inEntry == null) {
                m_InfoPage.gameObject.SetActive(false);
                m_NoSelectionGroup.SetActive(true);
                LoadEntryFacts(null);
                m_EntryToggleGroup.SetAllTogglesOff();
                Services.Data?.SetVariable("portable:bestiary.currentEntry", null);
                return;
            }

            Services.Data.SetVariable("portable:bestiary.currentEntry", m_CurrentEntry.Id());
            m_SelectCounter++;

            m_NoSelectionGroup.SetActive(false);
            m_InfoPage.gameObject.SetActive(true);

            if (inbSyncToggles) {
                foreach (var toggle in m_ListPools.AllEntries()) {
                    if (ReferenceEquals(toggle.Data, inEntry)) {
                        toggle.Toggle.SetIsOnWithoutNotify(true);
                        m_EntryScroll.ScrollYToShow((RectTransform) toggle.Toggle.transform);
                        break;
                    }
                }
            }

            Handler.PopulatePage(m_InfoPage, inEntry);
            LoadEntryFacts(inEntry);

            // periodically unload unused sketches in memory
            if (m_SelectCounter >= 10) {
                m_SelectCounter = 0;
                Streaming.UnloadUnusedAsync(10);
            }
        }

        /// <summary>
        /// Loads facts for the entry.
        /// </summary>
        private void LoadEntryFacts(BestiaryDesc inEntry) {
            m_InfoPage.FactPools.FreeAll();
            m_InstantiatedButtons.Clear();

            if (inEntry == null) {
                return;
            }

            using(PooledList<BFBase> facts = PooledList<BFBase>.Create()) {
                Save.Bestiary.GetFactsForEntity(inEntry.Id(), facts);
                if (facts.Count == 0) {
                    m_InfoPage.HasFacts.SetActive(false);
                    m_InfoPage.NoFacts.SetActive(true);
                } else {
                    m_InfoPage.NoFacts.SetActive(false);
                    m_InfoPage.HasFacts.SetActive(true);

                    facts.Sort(BFType.SortByVisualOrder);

                    Handler.PopulateFacts(m_InfoPage, inEntry, facts, FinalizeFactButton);
                }
            }

            m_InfoPage.FactLayout.ForceRebuild();

            foreach (var layoutfix in m_InfoPage.LayoutFixes)
                layoutfix.Rebuild();
        }

        private void FinalizeFactButton(BFBase inFact, MonoBehaviour inDisplay) {
            BestiaryFactButton factButton = inDisplay.GetComponent<BestiaryFactButton>();
            switch(m_Request.Type) {
                case PortableRequestType.SelectFact: {
                    bool bInteractable = m_Request.CanSelect != null ? m_Request.CanSelect(inFact) : true;
                    factButton.Initialize(inFact, true, bInteractable, m_CachedSelectFactDelegate ?? (m_CachedSelectFactDelegate = OnFactClicked));
                    break;
                }

                case PortableRequestType.SelectFactSet: {
                    bool bInteractable = m_Request.CanSelect != null ? m_Request.CanSelect(inFact) : true;
                    factButton.Initialize(inFact, true, bInteractable, m_CachedSelectFactDelegate ?? (m_CachedSelectFactDelegate = OnFactClicked));
                    break;
                }

                default: {
                    factButton.Initialize(inFact, true, true, m_CachedSelectFactDelegate ?? (m_CachedSelectFactDelegate = OnFactClicked));
                    break;
                }
            }
            
            m_InstantiatedButtons.Add(factButton);
        }

        private void UpdateAllFactInstances() {
            if (m_Request.CanSelect != null) {
                foreach(var button in m_InstantiatedButtons) {
                    button.UpdateInteractable(m_Request.CanSelect(button.Fact));
                }
            }
        }

        #endregion // Loading

        public override void HandleRequest(PortableRequest inRequest) {
            switch(inRequest.Type) {
                case PortableRequestType.ShowBestiary: {
                    LoadEntry(Assets.Bestiary(inRequest.TargetId), true);
                    break;
                }

                case PortableRequestType.ShowFact: {
                    LoadEntry(Assets.Fact(inRequest.TargetId).Parent, true);
                    break;
                }

                case PortableRequestType.SelectFact: {
                    m_Request = inRequest;
                    break;
                }

                case PortableRequestType.SelectFactSet: {
                    m_Request = inRequest;
                    break;
                }
            }
        }

        public override void ClearRequest() {
            m_Request = default;
        }
    }
}