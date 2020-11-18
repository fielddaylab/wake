using System;
using System.Collections.Generic;
using Aqua;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Portable
{
    [CreateAssetMenu(menuName = "Aqualab/Portable/Portable Tweaks")]
    public class PortableTweaks : TweakAsset
    {
        [SerializeField] private BestiaryEntry[] m_Entries = null;
        
        [Header("Colors")]
        [SerializeField] private Color m_CritterListColor = ColorBank.Orange;
        [SerializeField] private Color m_EcosystemListColor = ColorBank.Aqua;

        private Dictionary<StringHash32, BestiaryEntry> m_BestiaryMap = new Dictionary<StringHash32, BestiaryEntry>();

        protected override void Apply()
        {
            base.Apply();

            m_BestiaryMap = KeyValueUtils.CreateMap<StringHash32, BestiaryEntry, BestiaryEntry>(m_Entries);
        }

        public BestiaryEntry BestiaryEntryById(StringHash32 inId)
        {
            BestiaryEntry entry;
            m_BestiaryMap.TryGetValue(inId, out entry);
            return entry;
        }

        public IReadOnlyList<BestiaryEntry> AllBestiaryEntries()
        {
            return m_Entries;
        }

        public IEnumerable<BestiaryEntry> AllBestiaryEntriesForType(BestiaryEntryType inType)
        {
            for(int i = 0; i < m_Entries.Length; ++i)
            {
                if (m_Entries[i].Type == inType)
                    yield return m_Entries[i];
            }
        }

        public Color BestiaryListColor(BestiaryEntryType inEntryType)
        {
            switch(inEntryType)
            {
                case BestiaryEntryType.Critter:
                    return m_CritterListColor;
                case BestiaryEntryType.Ecosystem:
                    return m_EcosystemListColor;

                default:
                    throw new ArgumentOutOfRangeException("inEntryType");
            }
        }
    }
}