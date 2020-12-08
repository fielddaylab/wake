using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Entry", fileName = "NewBestiaryEntry")]
    public class BestiaryDesc : DBObject
    {
        #region Inspector

        [SerializeField] private BestiaryDescCategory m_Type = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_Flags = 0;

        [Header("Info")]
        [SerializeField] private string m_ScientificNameId;
        [SerializeField] private string m_CommonNameId;
        
        [Space]
        [SerializeField] private BestiaryFactBase[] m_Facts = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private Sprite m_Sketch = null;
        [SerializeField] private SerializedHash32 m_ListenAudioEvent;

        #endregion // Inspector

        [SerializeField] private Dictionary<StringHash32, BestiaryFactBase> m_FactMap;

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }

        public string ScientificName() { return m_ScientificNameId; }
        public string CommonName() { return m_CommonNameId; }

        public IReadOnlyList<BestiaryFactBase> Facts { get { return m_Facts; } }

        public BestiaryFactBase Fact(StringHash32 inFactId)
        {
            BestiaryFactBase fact;
            m_FactMap.TryGetValue(inFactId, out fact);
            return fact;
        }

        public TFact Fact<TFact>(StringHash32 inFactId) where TFact : BestiaryFactBase
        {
            return (TFact) Fact(inFactId);
        }

        public Sprite Icon { get { return m_Icon; } }
        public Sprite Sketch { get { return m_Sketch; } }

        public StringHash32 ListenAudio { get { return m_ListenAudioEvent; } }

        public void Initialize()
        {
            m_FactMap = new Dictionary<StringHash32, BestiaryFactBase>();
            foreach(var fact in m_Facts)
            {
                fact.Hook(this);
                m_FactMap.Add(fact.Id(), fact);
            }
        }
    }

    public enum BestiaryDescCategory
    {
        Critter,
        Ecosystem
    }

    [Flags]
    public enum BestiaryDescFlags
    {
        Rare = 0x01,
    }
}