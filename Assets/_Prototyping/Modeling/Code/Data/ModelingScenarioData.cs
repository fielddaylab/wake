using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    [CreateAssetMenu(menuName = "Aqualab/Modeling/Scenario Data", fileName = "NewModelingScenario")]
    public sealed class ModelingScenarioData : ScriptableObject
    {
        private enum DuplicatedWaterPropertyId : byte
        {
            Oxygen,
            Temperature,
            Light,
            PH,
            CarbonDioxide,
        }

        #region Inspector

        [SerializeField] private BestiaryDesc m_Environment = null;
        [SerializeField] private uint m_Seed = 54321;
        
        [Header("Historical Data")]
        [SerializeField] private ActorCountU32[] m_InitialActors = null;
        [SerializeField] private uint m_TickCount = 1000;
        [SerializeField] private int m_TickScale = 10;

        [Header("Prediction")]
        [SerializeField] private uint m_PredictionTicks = 0;
        [SerializeField] private ActorCountRange[] m_TargetActors = null;
        [SerializeField] private ActorCountI32[] m_AdjustableActors = null;

        [Header("Water Properties")]
        [SerializeField] private bool m_DisplayWaterProperties = false;
        [SerializeField, ShowIfField("m_DisplayWaterProperties")] private DuplicatedWaterPropertyId m_PropertyToDisplay = DuplicatedWaterPropertyId.Light;

        [Header("Labels")]
        [SerializeField] private TextId m_TitleId = null;
        [SerializeField] private TextId m_DescId = null;
        [SerializeField] private TextId m_CompleteId = null;
        [SerializeField] private TextId m_TickLabelId = null;

        [Header("Results")]
        [SerializeField] private SerializedHash32 m_BestiaryModelId = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Optimized;
        [NonSerialized] private List<BestiaryDesc> m_Critters;
        [NonSerialized] private HashSet<StringHash32> m_AllowedLines;

        public uint Seed() { return m_Seed; }

        public BestiaryDesc Environment() { return m_Environment; }
        public ListSlice<BestiaryDesc> Critters() { Optimize(); return m_Critters; }
        public ListSlice<ActorCountU32> Actors() { Optimize(); return m_InitialActors; }

        public uint TickCount() { return m_TickCount; }
        public int TickScale() { return m_TickScale; }

        public uint PredictionTicks() { return m_PredictionTicks; }
        public ListSlice<ActorCountRange> PredictionTargets() { return m_TargetActors; }
        public ListSlice<ActorCountI32> AdjustableActors() { return m_AdjustableActors; }

        public bool DisplayWaterProperties() { return m_DisplayWaterProperties; }
        public WaterPropertyId WaterProperty() { return m_DisplayWaterProperties ? (WaterPropertyId) m_PropertyToDisplay : WaterPropertyId.NONE; }

        public TextId TitleId() { return m_TitleId; }
        public TextId DescId() { return m_DescId; }
        public TextId CompleteId() { return m_CompleteId; }
        public TextId TickLabelId() { return m_TickLabelId; }

        public StringHash32 BestiaryModelId() { return m_BestiaryModelId; }
        
        public uint TotalTicks() { return m_TickCount + m_PredictionTicks; }

        public bool ShouldGraph(StringHash32 inId)
        {
            return true;
        }

        public bool IsInHistorical(StringHash32 inId)
        {
            foreach(var actorCount in m_InitialActors)
            {
                if (actorCount.Id == inId)
                    return true;
            }

            return false;
        }

        private void Optimize()
        {
            if (m_Optimized)
                return;

            BestiaryDesc critter;

            m_AllowedLines = new HashSet<StringHash32>();
            m_Critters = new List<BestiaryDesc>(m_InitialActors.Length);
            for(int i = 0; i < m_InitialActors.Length; ++i)
            {
                critter = Services.Assets.Bestiary.Get(m_InitialActors[i].Id);

                m_AllowedLines.Add(m_InitialActors[i].Id);
                m_Critters.Add(critter);
            }

            for(int i = 0; i < m_AdjustableActors.Length; ++i)
            {
                critter = Services.Assets.Bestiary.Get(m_AdjustableActors[i].Id);

                m_AllowedLines.Add(m_AdjustableActors[i].Id);
                if (!m_Critters.Contains(critter))
                    m_Critters.Add(critter);
            }

            KeyValueUtils.SortByKey<StringHash32, uint, ActorCountU32>(m_InitialActors);
            KeyValueUtils.SortByKey<StringHash32, ActorCountRange>(m_TargetActors);
            
            m_Optimized = true;
        }
    }
}