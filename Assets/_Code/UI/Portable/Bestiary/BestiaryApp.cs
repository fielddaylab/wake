using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua;

namespace Aqua.Portable
{
    public class BestiaryApp : PortableMenuApp
    {
        #region Types

        [Serializable] private class EntryPool : SerializablePool<PortableListElement> { }
        [Serializable] private class FactPool : SerializablePool<BestiaryFactButton> { }

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
        }

        #endregion // Types

        #region Inspector

        [Header("Types")]
        [SerializeField, Required] private ToggleGroup m_EntryTypeToggleGroup = null;
        [SerializeField, Required] private Toggle m_CritterGroupToggle = null;
        [SerializeField, Required] private Toggle m_EcosystemGroupToggle = null;

        [Header("Entries")]
        [SerializeField, Required] private VerticalLayoutGroup m_EntryLayoutGroup = null;
        [SerializeField, Required] private ToggleGroup m_EntryToggleGroup = null;
        [SerializeField] private EntryPool m_EntryPool = null;

        [Header("Group")]
        [SerializeField, Required] private RectTransform m_NoSelectionGroup = null;
        [SerializeField, Required] private RectTransform m_HasSelectionGroup = null;

        [Header("Info")]
        [SerializeField, Required] private LocText m_ScientificNameLabel = null;
        [SerializeField, Required] private LocText m_CommonNameLabel = null;
        [SerializeField, Required] private Image m_SketchImage = null;
        [SerializeField, Required] private VerticalLayoutGroup m_FactLayoutGroup = null;
        [SerializeField] private FactPool m_FactPool = null;

        #endregion // Inspector

        [NonSerialized] private PortableTweaks m_Tweaks = null;
        [NonSerialized] private bool m_ArgumentationMode = false;
        [NonSerialized] private BestiaryDescCategory m_CurrentEntryGroup = BestiaryDescCategory.Critter;

        protected override void Awake()
        {
            base.Awake();

            m_CritterGroupToggle.onValueChanged.AddListener(OnCritterToggled);
            m_EcosystemGroupToggle.onValueChanged.AddListener(OnEcosystemToggled);
        }

        private void OnCritterToggled(bool inbOn)
        {
            if (!IsShowing())
                return;

            if (inbOn)
                LoadEntryGroup(BestiaryDescCategory.Critter, null, false);
        }

        private void OnEcosystemToggled(bool inbOn)
        {
            if (!IsShowing())
                return;
                
            if (inbOn)
                LoadEntryGroup(BestiaryDescCategory.Ecosystem, null, false);
        }

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

                case BestiaryDescCategory.Ecosystem:
                    m_EcosystemGroupToggle.SetIsOnWithoutNotify(true);
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

        private void LoadEntry(BestiaryDesc inEntry)
        {
            m_FactPool.Reset();

            foreach(var button in m_EntryPool.ActiveObjects)
            {
                button.SetState((BestiaryDesc) button.Data == inEntry);
            }

            if (inEntry == null)
            {
                m_NoSelectionGroup.gameObject.SetActive(true);
                m_HasSelectionGroup.gameObject.SetActive(false);
                return;
            }

            m_NoSelectionGroup.gameObject.SetActive(false);
            m_HasSelectionGroup.gameObject.SetActive(true);

            m_ScientificNameLabel.SetText(inEntry.ScientificName());
            m_CommonNameLabel.SetText(inEntry.CommonName());

            m_SketchImage.sprite = inEntry.Sketch();
            m_SketchImage.gameObject.SetActive(inEntry.Sketch());

            foreach(var fact in Services.Data.Profile.Bestiary.GetFactsForEntity(inEntry.Id()))
            {
                BestiaryFactButton factButton = m_FactPool.Alloc();
                factButton.Initialize(fact.Fact, fact, m_ArgumentationMode);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_FactLayoutGroup.transform);
        }

        public override bool TryHandle(IPortableRequest inRequest)
        {
            OpenToRequest openTo = inRequest as OpenToRequest;
            if (openTo != null)
            {
                Show();
                LoadTarget(openTo.Target);
                return true;
            }
            
            return false;
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            m_Tweaks = Services.Tweaks.Get<PortableTweaks>();

            m_CritterGroupToggle.SetIsOnWithoutNotify(true);
            m_EcosystemGroupToggle.SetIsOnWithoutNotify(false);
            LoadEntryGroup(BestiaryDescCategory.Critter, null, true);

            LoadEntry(null);
        }

        protected override void OnHide(bool inbInstant)
        {
            m_FactPool.Reset();
            m_NoSelectionGroup.gameObject.SetActive(true);
            m_HasSelectionGroup.gameObject.SetActive(false);

            m_FactPool.Reset();
            m_EntryPool.Reset();
            m_EntryToggleGroup.SetAllTogglesOff(false);
            
            m_ArgumentationMode = false;

            base.OnHide(inbInstant);
        }
    }
}