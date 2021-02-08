using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Entry", fileName = "NewBestiaryEntry")]
    public class BestiaryDesc : DBObject
    {
        private struct VariantPair : IKeyValuePair<StringHash32, string>
        {
            public SerializedHash32 Id;
            public string Name;

            public StringHash32 Key { get { return Id; } }

            public string Value { get { return Name; } }

            public VariantPair(StringHash32 inId, string inName)
            {
                Id = inId;
                Name = inName;
            }
        }

        #region Inspector

        [SerializeField, AutoEnum] private BestiaryDescCategory m_Type = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_Flags = 0;
        [SerializeField, AutoEnum] private BestiaryDescSize m_Size = 0;

        [Header("Info")]
        [SerializeField] private string m_ScientificNameId = null;
        [SerializeField] private string m_CommonNameId = null;

        [Space]
        [SerializeField] private VariantPair[] m_VariantIds = null;
        
        [Space]
        [SerializeField] private BFBase[] m_Facts = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private Sprite m_Sketch = null;
        [SerializeField] private SerializedHash32 m_ListenAudioEvent = null;

        #endregion // Inspector

        [SerializeField] private Dictionary<StringHash32, BFBase> m_FactMap;
        [NonSerialized] private BFBase[] m_InternalFacts;
        [NonSerialized] private BFBase[] m_AssumedFacts;

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        public string ScientificName() { return m_ScientificNameId; }
        public string CommonName() { return m_CommonNameId; }

        public string VariantName(StringHash32 inVariantId)
        {
            string value;
            if (!m_VariantIds.TryGetValue(inVariantId, out value))
            {
                Debug.LogErrorFormat("[BestiaryDesc] No variant name for id '{0}' found on bestiary entry '{1}'", inVariantId.ToDebugString(), Id().ToDebugString());
            }
            return value;
        }

        public IReadOnlyList<BFBase> Facts { get { return m_Facts; } }
        public IReadOnlyList<BFBase> InternalFacts { get { return m_InternalFacts; } }
        public IReadOnlyList<BFBase> AssumedFacts { get { return m_AssumedFacts; } }

        public BFBase Fact(StringHash32 inFactId)
        {
            BFBase fact;
            m_FactMap.TryGetValue(inFactId, out fact);
            return fact;
        }

        public TFact Fact<TFact>(StringHash32 inFactId) where TFact : BFBase
        {
            return (TFact) Fact(inFactId);
        }

        public Sprite Icon() { return m_Icon; }
        public Sprite Sketch() { return m_Sketch; }

        public StringHash32 ListenAudio() { return m_ListenAudioEvent; }

        public void Initialize()
        {
            using(PooledList<BFBase> internalFacts = PooledList<BFBase>.Create())
            using(PooledList<BFBase> assumedFacts = PooledList<BFBase>.Create())
            {
                m_FactMap = new Dictionary<StringHash32, BFBase>();
                foreach(var fact in m_Facts)
                {
                    fact.Hook(this);
                    m_FactMap.Add(fact.Id(), fact);

                    switch(fact.Mode())
                    {
                        case BFMode.Internal:
                            internalFacts.Add(fact);
                            break;

                        case BFMode.Always:
                            assumedFacts.Add(fact);
                            break;
                    }
                }

                m_InternalFacts = internalFacts.ToArray();
                m_AssumedFacts = assumedFacts.ToArray();
            }
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            switch(m_Type)
            {
                case BestiaryDescCategory.Critter:
                    {
                        if (m_Size == BestiaryDescSize.Ecosystem)
                            m_Size = BestiaryDescSize.Large;
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        if (m_Size != BestiaryDescSize.Ecosystem)
                            m_Size = BestiaryDescSize.Ecosystem;
                        break;
                    }
            }
        }

        #endif // UNITY_EDITOR
    }

    public enum BestiaryDescCategory
    {
        Critter,
        Environment
    }

    [Flags]
    public enum BestiaryDescFlags
    {
        Rare = 0x01,
        LargeCreature = 0x02,
        DoNotUseInExperimentation = 0x04,
        IsVariant = 0x08,
    }

    public enum BestiaryDescSize
    {
        Tiny,
        Small,
        Medium,
        Large,

        Ecosystem = 8
    }
}