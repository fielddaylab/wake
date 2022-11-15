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

    [RequireComponent(typeof(BestiaryApp))]
    public class SpectersApp : MonoBehaviour {

        #region Inspector

        [Header("Fact Grouping")]
        [SerializeField] private LocText m_EncodedMessageText = null;
        [SerializeField] private RectTransform m_StateFactHeader = null;
        [SerializeField] private RectTransform m_BehaviorFactHeader = null;
        [SerializeField] private RectTransform m_FactListSpacing = null;

        #endregion // Inspector
        
        [NonSerialized] private List<TMP_Text> m_CachedFactTextList = new List<TMP_Text>();

        private void Awake() {
            GetComponent<BestiaryApp>().Handler = new BestiaryApp.DisplayHandler() {
                Category = BestiaryDescCategory.Critter,
                GetEntries = GetEntries,
                PopulateToggle = PopulateEntryToggle,
                PopulatePage = PopulateEntryPage,
                PopulateFacts = PopulateEntryFacts,
            };
        }

        static private void GetEntries(BestiaryDescCategory category, List<TaggedBestiaryDesc> entries) {
            var ordering = Services.Tweaks.Get<ScienceTweaks>().CanonicalSpecterOrdering();

            foreach(var organism in ordering) {
                if (!Save.Bestiary.HasEntity(organism.Id())) {
                    continue;
                }

                entries.Add(new TaggedBestiaryDesc(organism, null));
            }
        }

        static private void PopulateEntryToggle(PortableBestiaryToggle toggle, BestiaryDesc entry) {
            toggle.Icon.sprite = entry.Icon();
            toggle.Icon.gameObject.SetActive(true);

            if (Save.Science.FullyDecrypted()) {
                toggle.Cursor.TooltipId = entry.CommonName();
                toggle.Cursor.TooltipOverride = null;
                toggle.Text.SetText(entry.CommonName());
            } else {
                string commonNameScrambled = Formatting.ScrambleLoc(entry.CommonName());
                toggle.Cursor.TooltipId = null;
                toggle.Cursor.TooltipOverride = commonNameScrambled;
                toggle.Text.SetTextNoParse(commonNameScrambled);
            }

            toggle.Text.Graphic.rectTransform.offsetMin = new Vector2(38, 4);
        }

        static private void PopulateEntryPage(BestiaryPage page, BestiaryDesc entry) {
            if (Save.Science.FullyDecrypted()) {
                page.ScientificName.SetTextFromString(entry.ScientificName());
                page.CommonName.SetText(entry.CommonName());
                page.Description.SetText(entry.Description());
            } else {
                page.ScientificName.SetTextNoParse(Formatting.Scramble(entry.ScientificName()));
                page.CommonName.SetTextNoParse(Formatting.ScrambleLoc(entry.CommonName()));
                page.Description.SetTextNoParse(Formatting.ScrambleLoc(entry.Description()));
            }
            
            page.Sketch.Display(entry.ImageSet());
        }

        private void PopulateEntryFacts(BestiaryPage page, BestiaryDesc entry, ListSlice<BFBase> facts, BestiaryApp.FinalizeButtonDelegate finalizeCallback) {
            bool bState = false, bBehavior = false;

            bool encrypt = !Save.Science.FullyDecrypted();

            if (!encrypt) {
                m_EncodedMessageText.SetText(entry.EncodedMessage());
            } else {
                m_EncodedMessageText.SetTextNoParse(Formatting.ScrambleLoc(entry.EncodedMessage()));
            }

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

                MonoBehaviour factDisplay = page.FactPools.Alloc(fact,
                    Save.Bestiary.GetDiscoveredFlags(fact.Id),
                    entry, page.FactLayout.transform);

                finalizeCallback(fact, factDisplay);
            }
        }
    }
}