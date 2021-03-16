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
        #region Inspector

        [SerializeField, AutoEnum] private BestiaryDescCategory m_Type = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_Flags = 0;
        [SerializeField, AutoEnum] private BestiaryDescSize m_Size = 0;

        [Header("Info")]
        [SerializeField] private string m_ScientificNameId = null;
        [SerializeField] private string m_CommonNameId = null;
        
        [Space]
        [SerializeField] private BFBase[] m_Facts = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private Sprite m_Sketch = null;
        [SerializeField] private Color m_Color = ColorBank.White;
        [SerializeField] private SerializedHash32 m_ListenAudioEvent = null;

        #endregion // Inspector

        [SerializeField] private Dictionary<StringHash32, BFBase> m_FactMap;
        [NonSerialized] private BFBase[] m_InternalFacts;
        [NonSerialized] private BFBase[] m_AssumedFacts;
        [NonSerialized] private BFState[] m_StateChangeFacts;

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        public string ScientificName() { return m_ScientificNameId; }
        public string CommonName() { return m_CommonNameId; }
        public string PluralName() { return m_CommonNameId + "s"; }

        public IReadOnlyList<BFBase> Facts { get { return m_Facts; } }
        public IReadOnlyList<BFBase> InternalFacts { get { return m_InternalFacts; } }
        public IReadOnlyList<BFBase> AssumedFacts { get { return m_AssumedFacts; } }
        public IReadOnlyList<BFState> StateFacts { get { return m_StateChangeFacts; } }

        public bool HasCategory(BestiaryDescCategory inCategory)
        {
            return inCategory == BestiaryDescCategory.BOTH || m_Type == inCategory;
        }

        public bool HasFlags(BestiaryDescFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(BestiaryDescFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public Sprite Icon() { return m_Icon; }
        public Sprite Sketch() { return m_Sketch; }
        public Color Color() { return m_Color; }

        public StringHash32 ListenAudio() { return m_ListenAudioEvent; }

        public void Initialize()
        {
            using(PooledList<BFBase> internalFacts = PooledList<BFBase>.Create())
            using(PooledList<BFBase> assumedFacts = PooledList<BFBase>.Create())
            using(PooledList<BFState> stateFacts = PooledList<BFState>.Create())
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

                    BFState state;
                    if ((state = fact as BFState) != null)
                    {
                        stateFacts.Add(state);
                    }
                }

                m_InternalFacts = internalFacts.ToArray();
                m_AssumedFacts = assumedFacts.ToArray();
                m_StateChangeFacts = stateFacts.ToArray();
            }
        }

        #region Facts

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

        public TFact FactOfType<TFact>() where TFact : BFBase
        {
            TFact result;
            foreach(var fact in m_Facts)
            {
                if ((result = fact as TFact) != null)
                    return result;
            }

            return null;
        }

        public IEnumerable<TFact> FactsOfType<TFact>() where TFact : BFBase
        {
            TFact result;
            foreach(var fact in m_Facts)
            {
                if ((result = fact as TFact) != null)
                    yield return result;
            }
        }

        #endregion // Facts

        #region Checks

        public ActorStateId GetStateForEnvironment(in WaterPropertyBlockF32 inEnvironment)
        {
            // param: in WaterPropertyBlockU8 inStarvation)
            ActorStateId actorState = ActorStateId.Alive;

            for(int i = m_StateChangeFacts.Length - 1; i >= 0 && actorState < ActorStateId.Dead; --i)
            {
                BFState state = m_StateChangeFacts[i];
                
                // BFStateStarvation starve = state as BFStateStarvation;
                // if (!starve.IsReferenceNull())
                // {
                //     if (inStarvation[starve.PropertyId()] >= starve.Ticks())
                //     {
                //         actorState = starve.TargetState();
                //     }
                //     continue;
                // }

                BFStateRange range = state as BFStateRange;
                if (!range.IsReferenceNull())
                {
                    float currentVal = inEnvironment[range.PropertyId()];
                    if (currentVal < range.MinSafe() || currentVal > range.MaxSafe())
                    {
                        actorState = range.TargetState();
                    }
                }
            }

            return actorState;
        }

        #endregion // Checks

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

    [LabeledEnum]
    public enum BestiaryDescCategory
    {
        Critter,
        Environment,
        Model,

        [Hidden]
        BOTH
    }

    [Flags]
    public enum BestiaryDescFlags
    {
        Rare = 0x01,
        LargeCreature = 0x02,
        DoNotUseInExperimentation = 0x04,
        TreatAsHerd = 0x08
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