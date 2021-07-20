using System;
using System.Collections.Generic;
using System.IO;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Entry", fileName = "NewBestiaryEntry")]
    public class BestiaryDesc : DBObject, IComparable<BestiaryDesc>
    {
        #region Inspector

        [SerializeField, AutoEnum] private BestiaryDescCategory m_Type = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_Flags = 0;
        [SerializeField, AutoEnum] private BestiaryDescSize m_Size = 0;

        [Header("Info")]
        [SerializeField, ShowIfField("IsCritter")] private BestiaryDesc m_ParentEnvironment = null;
        [SerializeField, ShowIfField("IsCritter")] private string m_ScientificNameId = null;
        [SerializeField] private TextId m_CommonNameId = null;
        
        [Space]
        [SerializeField] private BFBase[] m_Facts = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, ShowIfField("ShowSketch")] private Sprite m_Sketch = null;
        [SerializeField] private Color m_Color = ColorBank.White;
        [SerializeField, ShowIfField("IsCritter")] private SerializedHash32 m_ListenAudioEvent = null;

        #endregion // Inspector

        [NonSerialized] private Dictionary<StringHash32, BFBase> m_FactMap;
        [NonSerialized] private BFBase[] m_AllFacts;
        [NonSerialized] private BFBase[] m_InternalFacts;
        [NonSerialized] private BFBase[] m_AssumedFacts;
        [NonSerialized] private BFState[] m_StateChangeFacts;
        [NonSerialized] private List<BFBase> m_ReciprocalFacts;
        [NonSerialized] private List<BestiaryDesc> m_ChildCritters;

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        public BestiaryDesc ParentEnvironment()
        {
            Assert.True(m_Type == BestiaryDescCategory.Critter, "BestiaryDesc '{0}' is not a critter!", Id());
            return m_ParentEnvironment;
        }
        public IReadOnlyList<BestiaryDesc> ChildCritters
        {
            get
            {
                Assert.True(m_Type == BestiaryDescCategory.Environment, "BestiaryDesc '{0}' is not an environment!", Id());
                return m_ChildCritters;
            }
        }

        public string ScientificName() { return m_ScientificNameId; }
        public TextId CommonName() { return m_CommonNameId; }

        public IReadOnlyList<BFBase> Facts { get { return m_AllFacts; } }
        public IReadOnlyList<BFBase> InternalFacts { get { return m_InternalFacts; } }
        public IReadOnlyList<BFBase> AssumedFacts { get { return m_AssumedFacts; } }
        public IReadOnlyList<BFState> StateFacts { get { return m_StateChangeFacts; } }

        internal IReadOnlyList<BFBase> SelfFacts { get { return m_Facts; } }

        public bool HasCategory(BestiaryDescCategory inCategory)
        {
            return inCategory == BestiaryDescCategory.ALL || m_Type == inCategory;
        }

        public bool HasFlags(BestiaryDescFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(BestiaryDescFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public Sprite Icon() { return m_Icon; }

        public Sprite Sketch() { return m_Sketch; }
        public Color Color() { return m_Color; }

        public StringHash32 ListenAudio() { return m_ListenAudioEvent; }

        internal void Initialize()
        {
            if (m_Type == BestiaryDescCategory.Environment)
            {
                if (m_ChildCritters == null)
                {
                    m_ChildCritters = new List<BestiaryDesc>();
                }
            }

            foreach(var fact in m_Facts)
            {
                BFEat eat = fact as BFEat;
                if (eat != null)
                {
                    BestiaryDesc reciprocal = eat.Target();
                    if (reciprocal.m_ReciprocalFacts == null)
                    {
                        reciprocal.m_ReciprocalFacts = new List<BFBase>();
                    }
                    reciprocal.m_ReciprocalFacts.Add(eat);
                }
            }

            if (m_ParentEnvironment != null)
            {
                if (m_ParentEnvironment.m_ChildCritters == null)
                {
                    m_ParentEnvironment.m_ChildCritters = new List<BestiaryDesc>();
                }

                m_ParentEnvironment.m_ChildCritters.Add(this);
            }
        }

        internal void PostInitialize()
        {
            using(PooledList<BFBase> internalFacts = PooledList<BFBase>.Create())
            using(PooledList<BFBase> assumedFacts = PooledList<BFBase>.Create())
            using(PooledList<BFState> stateFacts = PooledList<BFState>.Create())
            {
                m_FactMap = new Dictionary<StringHash32, BFBase>();
                foreach(var fact in m_Facts)
                {
                    ProcessFact(fact, internalFacts, assumedFacts, stateFacts, true);
                }

                if (m_ReciprocalFacts != null)
                {
                    foreach(var fact in m_ReciprocalFacts)
                    {
                        ProcessFact(fact, internalFacts, assumedFacts, stateFacts, false);
                    }
                }

                m_InternalFacts = internalFacts.ToArray();
                m_AssumedFacts = assumedFacts.ToArray();
                m_StateChangeFacts = stateFacts.ToArray();
                m_ReciprocalFacts?.Clear();
                m_ReciprocalFacts = null;

                m_AllFacts = new BFBase[m_FactMap.Count];
                m_FactMap.Values.CopyTo(m_AllFacts, 0);
            }
        }

        private void ProcessFact(BFBase inFact, List<BFBase> ioInternal, List<BFBase> ioAssumed, List<BFState> ioState, bool inbHook)
        {
            Assert.NotNull(inFact, "Null fact on BestiaryDesc '{0}'", name);

            if (inbHook)
                inFact.Hook(this);

            m_FactMap.Add(inFact.Id(), inFact);

            switch(inFact.Mode())
            {
                case BFMode.Internal:
                    ioInternal.Add(inFact);
                    break;

                case BFMode.Always:
                    ioAssumed.Add(inFact);
                    break;
            }

            BFState state;
            if ((state = inFact as BFState) != null)
            {
                ioState.Add(state);
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
            foreach(var fact in m_AllFacts ?? m_Facts)
            {
                if ((result = fact as TFact) != null)
                    return result;
            }

            return null;
        }

        public IEnumerable<TFact> FactsOfType<TFact>() where TFact : BFBase
        {
            TFact result;
            foreach(var fact in m_AllFacts ?? m_Facts)
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
                BFState fact = m_StateChangeFacts[i];
                ActorStateId desiredState = fact.Range().Evaluate(inEnvironment[fact.PropertyId()]);
                if (desiredState > actorState)
                {
                    actorState = desiredState;
                }
            }

            return actorState;
        }

        #endregion // Checks

        #region IComparable

        int IComparable<BestiaryDesc>.CompareTo(BestiaryDesc other)
        {
            return Id().CompareTo(other.Id());
        }

        #endregion // IComparable

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

        private bool IsCritter()
        {
            return m_Type == BestiaryDescCategory.Critter;
        }

        private bool IsEnvironment()
        {
            return m_Type == BestiaryDescCategory.Environment;
        }

        private bool IsModels()
        {
            return m_Type == BestiaryDescCategory.Critter;
        }

        private bool ShowSketch()
        {
            return m_Type != BestiaryDescCategory.Model;
        }

        [ContextMenu("Load All In Directory")]
        private void FindAllFacts()
        {
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string myDirectory = Path.GetDirectoryName(myPath);
            m_Facts = ValidationUtils.FindAllAssets<BFBase>(myDirectory);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.CustomEditor(typeof(BestiaryDesc)), UnityEditor.CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Load All In Directory"))
                {
                    foreach(BestiaryDesc bestiary in targets)
                    {
                        bestiary.FindAllFacts();
                    }
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
        ALL
    }

    [Flags]
    public enum BestiaryDescFlags
    {
        Rare = 0x01,
        LargeCreature = 0x02,
        DoNotUseInExperimentation = 0x04,
        TreatAsHerd = 0x08,
        Human = 0x10,
        IgnoreStarvation = 0x20
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