using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable {

    [RequireComponent(typeof(BestiaryApp))]
    public class OrganismsApp : MonoBehaviour {

        #region Inspector

        [Header("Fact Grouping")]
        [SerializeField] private RectTransform m_StateFactHeader = null;
        [SerializeField] private RectTransform m_BehaviorFactHeader = null;
        [SerializeField] private RectTransform m_FactListSpacing = null;

        [Header("Environment Comparison")]
        [SerializeField] private TMP_Text m_EnvironmentLabel = null;
        [SerializeField] private Image m_EnvironmentIcon = null;
        [SerializeField] private Button m_NextEnvironmentButton = null;
        [SerializeField] private Button m_PrevEnvironmentButton = null;

        #endregion // Inspector

        [NonSerialized] private List<StateFactDisplay> m_StateFacts = new List<StateFactDisplay>(3);
        [NonSerialized] private BestiaryDesc m_ComparisonEnv = null;
        [NonSerialized] private List<BestiaryDesc> m_AvailableEnvironments = new List<BestiaryDesc>(8);

        static private readonly RingBuffer<TaggedBestiaryDesc> SortBuffer = new RingBuffer<TaggedBestiaryDesc>(16, RingBufferMode.Expand);

        private void Awake() {
            GetComponent<BestiaryApp>().Handler = new BestiaryApp.DisplayHandler() {
                Category = BestiaryDescCategory.Critter,
                GetEntries = GetEntries,
                PopulateToggle = PopulateEntryToggle,
                PopulatePage = PopulateEntryPage,
                PopulateFacts = PopulateEntryFacts,
                OnClearFacts = OnClearFacts
            };

            m_NextEnvironmentButton.onClick.AddListener(() => AdvanceSiteSelection(1));
            m_PrevEnvironmentButton.onClick.AddListener(() => AdvanceSiteSelection(-1));
        }

        private void OnEnable() {
            if (Script.IsLoading) {
                return;
            }

            m_AvailableEnvironments.Clear();
            Save.Bestiary.GetEntities(BestiaryDescCategory.Environment, m_AvailableEnvironments);
            m_AvailableEnvironments.Sort((a, b) => BestiaryDesc.SortByEnvironment(a, b));
            m_AvailableEnvironments.Insert(0, null);

            if (m_ComparisonEnv != null && !m_AvailableEnvironments.Contains(m_ComparisonEnv)) {
                m_ComparisonEnv = null;
            }

            LoadComparisonDisplay();
        }

        private void OnClearFacts() {
            m_StateFacts.Clear();
        }

        static private void GetEntries(BestiaryDescCategory category, List<TaggedBestiaryDesc> entries) {
            var ordering = Services.Tweaks.Get<ScienceTweaks>().CanonicalOrganismOrdering();

            SortBuffer.Clear();
            StringHash32 prevTag = null;

            foreach(var organism in ordering) {
                if (!organism.Tag.IsEmpty && organism.Tag != organism.Entity.StationId() && !Save.Map.HasVisitedLocation(organism.Tag)) {
                    continue;
                }

                if (!Save.Bestiary.HasEntity(organism.Entity.Id())) {
                    continue;
                }

                if (organism.Entity.HasFlags(BestiaryDescFlags.IsSpecter) && !Save.Science.FullyDecrypted()) {
                    continue;
                }

                if (organism.Tag != prevTag) {
                    prevTag = organism.Tag;

                    if (SortBuffer.Count > 0) {
                        SortBuffer.Sort((x, y) => BestiaryDesc.SortNaturalInStation(x.Entity, y.Entity));
                        foreach(var entry in SortBuffer) {
                            entries.Add(entry);
                        }
                        SortBuffer.Clear();
                    }
                }

                SortBuffer.PushBack(organism);
            }

            if (SortBuffer.Count > 0) {
                SortBuffer.Sort((x, y) => BestiaryDesc.SortNaturalInStation(x.Entity, y.Entity));
                foreach(var entry in SortBuffer) {
                    entries.Add(entry);
                }
                SortBuffer.Clear();
            }
        }

        static private void PopulateEntryToggle(PortableBestiaryToggle toggle, BestiaryDesc entry) {
            toggle.Icon.sprite = entry.Icon();
            toggle.Icon.gameObject.SetActive(true);

            toggle.Cursor.TooltipId = entry.CommonName();
            toggle.Cursor.TooltipOverride = null;

            toggle.Text.SetText(entry.CommonName());
            toggle.Text.Graphic.rectTransform.offsetMin = new Vector2(38, 4);
        }

        static private void PopulateEntryPage(BestiaryPage page, BestiaryDesc entry) {
            page.ScientificName.SetTextFromString(entry.ScientificName());
            page.CommonName.SetText(entry.CommonName());
            page.Description.SetText(entry.Description());
            page.Sketch.Display(entry.ImageSet());

            // TODO: display locations
        }

        private IEnumerator PopulateEntryFacts(BestiaryPage page, BestiaryDesc entry, ListSlice<BFBase> facts, BestiaryApp.FinalizeButtonDelegate finalizeCallback) {
            bool bState = false, bBehavior = false;

            m_StateFactHeader.gameObject.SetActive(false);
            m_BehaviorFactHeader.gameObject.SetActive(false);
            m_FactListSpacing.gameObject.SetActive(false);

            m_StateFacts.Clear();

            foreach (var fact in facts) {
                if (fact.Mode == BFMode.Internal) {
                    continue;
                }

                if (fact.Type == BFTypeId.State && !bState) {
                    m_StateFactHeader.gameObject.SetActive(true);
                    m_StateFactHeader.SetAsLastSibling();
                    bState = true;
                }

                if (fact.Type != BFTypeId.State && !bBehavior) {
                    if (bState) {
                        m_FactListSpacing.gameObject.SetActive(true);
                        m_FactListSpacing.SetAsLastSibling();
                    }

                    m_BehaviorFactHeader.gameObject.SetActive(true);
                    m_BehaviorFactHeader.SetAsLastSibling();
                    bBehavior = true;
                }

                MonoBehaviour factDisplay = page.FactPools.Alloc(fact,
                    Save.Bestiary.GetDiscoveredFlags(fact.Id),
                    entry, page.FactLayout.transform);

                if (fact.Type == BFTypeId.State) {
                    StateFactDisplay stateFact = (StateFactDisplay) factDisplay;
                    m_StateFacts.Add(stateFact);
                }

                finalizeCallback(fact, factDisplay);
                yield return null;
            }

            RefreshFactComparisons();
        }
    
        private void LoadComparisonDisplay() {
            if (m_ComparisonEnv == null) {
                m_EnvironmentIcon.sprite = SharedCanvasResources.DefaultWhiteSprite;
                m_EnvironmentIcon.color = AQColors.Teal;

                m_EnvironmentLabel.color = AQColors.Teal;
                m_EnvironmentLabel.SetText(Loc.Find("aqos.organism.noEnvSelected"));
            } else {
                m_EnvironmentIcon.sprite = m_ComparisonEnv.Icon();
                m_EnvironmentIcon.color = Color.white;

                m_EnvironmentLabel.SetText(BestiaryUtils.FullLabel(m_ComparisonEnv, false));
                m_EnvironmentLabel.color = m_ComparisonEnv.Color();
            }
        }

        private void RefreshFactComparisons() {
            foreach(var fact in m_StateFacts) {
                fact.SetEnvironment(m_ComparisonEnv);
            }
        }

        private void AdvanceSiteSelection(int advance) {
            int count = m_AvailableEnvironments.Count;

            int currentIndex = m_ComparisonEnv == null ? 0 : m_AvailableEnvironments.IndexOf(m_ComparisonEnv);
            if (currentIndex < 0) {
                currentIndex = 0;
            } else {
                currentIndex = (currentIndex + advance + count) % count;
            }

            m_ComparisonEnv = m_AvailableEnvironments[currentIndex];
            LoadComparisonDisplay();
            RefreshFactComparisons();
        }
    }
}