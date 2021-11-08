using System;
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

    [RequireComponent(typeof(BestiaryApp))]
    public class EcosystemsApp : MonoBehaviour {

        #region Inspector

        [Header("Fact Grouping")]
        [SerializeField] private RectTransform m_WaterChemistryHeader = null;
        [SerializeField] private RectTransform m_WaterChemistryGroup = null;
        [SerializeField] private RectTransform m_PopulationHeader = null;
        [SerializeField] private RectTransform m_PopulationGroup = null;
        [SerializeField] private RectTransform m_ModelsHeader = null;
        [SerializeField] private RectTransform[] m_GroupSpacings = null;

        #endregion // Inspector

        private void Awake() {
            GetComponent<BestiaryApp>().Handler = new BestiaryApp.DisplayHandler() {
                Category = BestiaryDescCategory.Environment,
                PopulateToggle = PopulateEntryToggle,
                PopulatePage = PopulateEntryPage,
                PopulateFacts = PopulateEntryFacts,
            };
        }

        static private void PopulateEntryToggle(PortableBestiaryToggle toggle, BestiaryDesc entry) {
            MapDesc map = Assets.Map(entry.DiveSiteId());
            string text = string.Format("{0}:\n{1}", Loc.Find(map.ShortLabelId()).ToUpper(), Loc.Find(entry.CommonName()));

            toggle.Cursor.TooltipId = entry.CommonName();
            toggle.Text.SetText(text);
        }

        static private void PopulateEntryPage(BestiaryPage page, BestiaryDesc entry) {
            MapDesc map = Assets.Map(entry.DiveSiteId());
            string text = string.Format("{0}: {1}", Loc.Find(map.ShortLabelId()), Loc.Find(entry.CommonName()));

            page.CommonName.SetText(text);
            page.Sketch.Display(new StreamedImageSet(entry.SketchPath()));
            page.Description.SetText(entry.Description());
        }

        private void PopulateEntryFacts(BestiaryPage page, BestiaryDesc entry, ListSlice<BFBase> facts, BestiaryApp.FinalizeButtonDelegate finalizeCallback) {
            m_ModelsHeader.gameObject.SetActive(false);
            m_PopulationHeader.gameObject.SetActive(false);
            m_WaterChemistryHeader.gameObject.SetActive(false);
            m_PopulationGroup.gameObject.SetActive(false);
            m_WaterChemistryGroup.gameObject.SetActive(false);

            bool bWaterChem = false, bPopulation = false, bModels = false;
            int spacingsUsed = 0;

            Transform target;
            foreach (var fact in facts) {
                if (fact.Mode == BFMode.Internal) {
                    continue;
                }

                target = page.FactLayout.transform;

                switch (fact.Type) {
                    case BFTypeId.WaterProperty:
                    case BFTypeId.WaterPropertyHistory: {
                            if (!bWaterChem) {
                                m_WaterChemistryHeader.gameObject.SetActive(true);
                                m_WaterChemistryHeader.SetAsLastSibling();
                                m_WaterChemistryGroup.gameObject.SetActive(true);
                                m_WaterChemistryGroup.SetAsLastSibling();
                                bWaterChem = true;
                            }
                            if (fact.Type == BFTypeId.WaterProperty) {
                                target = m_WaterChemistryGroup;
                            }
                            break;
                        }

                    case BFTypeId.Population:
                    case BFTypeId.PopulationHistory: {
                            if (!bPopulation) {
                                if (bWaterChem) {
                                    AllocSpacing(ref spacingsUsed);
                                }

                                m_PopulationHeader.gameObject.SetActive(true);
                                m_PopulationHeader.SetAsLastSibling();
                                m_PopulationGroup.gameObject.SetActive(true);
                                m_PopulationGroup.SetAsLastSibling();
                                bPopulation = true;
                            }
                            if (fact.Type == BFTypeId.Population) {
                                target = m_PopulationGroup;
                            }
                            break;
                        }

                    case BFTypeId.Model: {
                            if (!bModels) {
                                if (bWaterChem || bPopulation) {
                                    AllocSpacing(ref spacingsUsed);
                                }

                                m_ModelsHeader.gameObject.SetActive(true);
                                m_ModelsHeader.SetAsLastSibling();
                                bModels = true;
                            }
                            break;
                        }
                }

                MonoBehaviour factDisplay = page.FactPools.Alloc(fact, entry,
                    Services.Data.Profile.Bestiary.GetDiscoveredFlags(fact.Id),
                    target);

                finalizeCallback(fact, factDisplay);
            }

            for (; spacingsUsed < m_GroupSpacings.Length; spacingsUsed++) {
                m_GroupSpacings[spacingsUsed].gameObject.SetActive(false);
            }
        }

        private void AllocSpacing(ref int spacingsUsed) {
            RectTransform spacing = m_GroupSpacings[spacingsUsed++];
            spacing.gameObject.SetActive(true);
            spacing.SetAsLastSibling();
        }
    }
}