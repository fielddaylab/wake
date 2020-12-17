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
    public class JournalApp : PortableMenuApp
    {
        #region Types

        [Serializable] private class EntryPool : SerializablePool<PortableListElement> { }

        #endregion // Types

        #region Inspector

        [Header("Types")]
        [SerializeField, Required] private ToggleGroup m_EntryTypeToggleGroup = null;

        [Header("Entries")]
        [SerializeField, Required] private VerticalLayoutGroup m_EntryLayoutGroup = null;
        [SerializeField, Required] private ToggleGroup m_EntryToggleGroup = null;
        [SerializeField] private EntryPool m_EntryPool = null;

        [Header("Group")]
        [SerializeField, Required] private RectTransform m_NoSelectionGroup = null;
        [SerializeField, Required] private RectTransform m_HasSelectionGroup = null;

        [Header("Info")]
        [SerializeField, Required] private TMP_Text m_ScientificNameLabel = null;
        [SerializeField, Required] private TMP_Text m_CommonNameLabel = null;
        [SerializeField, Required] private Image m_SketchImage = null;
        // [SerializeField, Required] private VerticalLayoutGroup m_FactLayoutGroup = null;
        // [SerializeField] private FactPool m_FactPool = null;

        #endregion // Inspector

        [NonSerialized] private PortableTweaks m_Tweaks = null;

        // protected override void Awake()
        // {
        //     base.Awake();

        // }

        // private void OnCritterToggled(bool inbOn)
        // {
        //     if (inbOn)
        //         LoadEntryGroup(BestiaryEntryType.Critter, false);
        // }

        // private void OnEcosystemToggled(bool inbOn)
        // {
        //     if (inbOn)
        //         LoadEntryGroup(BestiaryEntryType.Ecosystem, false);
        // }

        // private void LoadEntryGroup(BestiaryEntryType inType, bool inbForce)
        // {
        //     if (!inbForce && m_CurrentEntryGroup == inType)
        //         return;

        //     m_CurrentEntryGroup = inType;
        //     m_EntryPool.Reset();

        //     Color buttonColor = m_Tweaks.BestiaryListColor(inType);
        //     foreach(var entry in m_Tweaks.AllBestiaryEntriesForType(inType))
        //     {
        //         PortableListElement button = m_EntryPool.Alloc();
        //         button.Initialize(entry.Icon, buttonColor, m_EntryToggleGroup, entry.CommonName, entry, OnEntryToggled);
        //     }

        //     LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_EntryLayoutGroup.transform);

        //     LoadEntry(null);
        // }

        // private void OnEntryToggled(PortableListElement inElement, bool inbOn)
        // {
        //     if (!inbOn)
        //     {
        //         if (!m_EntryToggleGroup.AnyTogglesOn())
        //             LoadEntry(null);
        //         return;
        //     }

        //     LoadEntry((BestiaryEntry) inElement.Data);
        // }

        // private void LoadEntry(BestiaryEntry inEntry)
        // {
        //     m_FactPool.Reset();

        //     if (inEntry == null)
        //     {
        //         m_NoSelectionGroup.gameObject.SetActive(true);
        //         m_HasSelectionGroup.gameObject.SetActive(false);
        //         return;
        //     }

        //     m_NoSelectionGroup.gameObject.SetActive(false);
        //     m_HasSelectionGroup.gameObject.SetActive(true);

        //     m_ScientificNameLabel.SetText(inEntry.ScientificName);
        //     m_CommonNameLabel.SetText(inEntry.CommonName);

        //     m_SketchImage.sprite = inEntry.Sketch;
        //     m_SketchImage.gameObject.SetActive(inEntry.Sketch);

        //     foreach(var fact in inEntry.Facts)
        //     {
        //         BestiaryFactButton factButton = m_FactPool.Alloc();
        //         factButton.Initialize(fact, m_ArgumentationMode);
        //     }

        //     LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_FactLayoutGroup.transform);
        // }

        // protected override void OnShow(bool inbInstant)
        // {
        //     base.OnShow(inbInstant);

        //     m_Tweaks = Services.Tweaks.Get<PortableTweaks>();

        //     m_CritterGroupToggle.SetIsOnWithoutNotify(true);
        //     m_EcosystemGroupToggle.SetIsOnWithoutNotify(false);
        //     LoadEntryGroup(BestiaryEntryType.Critter, true);

        //     LoadEntry(null);
        // }

        // protected override void OnHide(bool inbInstant)
        // {
        //     m_FactPool.Reset();
        //     m_NoSelectionGroup.gameObject.SetActive(true);
        //     m_HasSelectionGroup.gameObject.SetActive(false);

        //     m_FactPool.Reset();
        //     m_EntryPool.Reset();
        //     m_EntryToggleGroup.SetAllTogglesOff(false);

        //     base.OnHide(inbInstant);
        // }
    }
}