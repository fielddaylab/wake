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
    public class OrganismsApp : MonoBehaviour {

        #region Inspector

        [Header("Fact Grouping")]
        [SerializeField] private RectTransform m_StateFactHeader = null;
        [SerializeField] private RectTransform m_BehaviorFactHeader = null;
        [SerializeField] private RectTransform m_FactListSpacing = null;

        #endregion // Inspector

        private void Awake() {
            GetComponent<BestiaryApp>().Handler = new BestiaryApp.DisplayHandler() {
                Category = BestiaryDescCategory.Critter,
                PopulateToggle = PopulateEntryToggle,
                PopulatePage = PopulateEntryPage,
                PopulateFacts = PopulateEntryFacts,
            };
        }

        static private void PopulateEntryToggle(PortableBestiaryToggle toggle, BestiaryDesc entry) {
            toggle.Icon.sprite = entry.Icon();
            toggle.Cursor.TooltipId = entry.CommonName();
            toggle.Text.SetText(entry.CommonName());
        }

        static private void PopulateEntryPage(BestiaryPage page, BestiaryDesc entry) {
            page.ScientificName.SetText(entry.ScientificName());
            page.CommonName.SetText(entry.CommonName());
            page.Description.SetText(entry.Description());
            page.Sketch.Display(entry.ImageSet());

            // TODO: display locations
        }

        private void PopulateEntryFacts(BestiaryPage page, BestiaryDesc entry, ListSlice<BFBase> facts, BestiaryApp.FinalizeButtonDelegate finalizeCallback) {
            bool bState = false, bBehavior = false;

            m_StateFactHeader.gameObject.SetActive(false);
            m_BehaviorFactHeader.gameObject.SetActive(false);
            m_FactListSpacing.gameObject.SetActive(false);

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

                MonoBehaviour factDisplay = page.FactPools.Alloc(fact, entry,
                    Save.Bestiary.GetDiscoveredFlags(fact.Id),
                    page.FactLayout.transform);

                finalizeCallback(fact, factDisplay);
            }
        }
    }
}