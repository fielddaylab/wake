using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    // [CreateAssetMenu(menuName = "Aqualab Content/Modeling Scenario Data", fileName = "NewModelingScenario")]
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

        [SerializeField, FilterBestiary(BestiaryDescCategory.Environment)] private BestiaryDesc m_Environment = null;
        [SerializeField] private uint m_Seed = 54321;
        
        [Header("Historical Data")]
        [SerializeField, KeyValuePair("Id", "Population")] private ActorCountU32[] m_InitialActors = null;
        [SerializeField] private uint m_TickCount = 1000;
        [SerializeField] private int m_TickScale = 10;
        [SerializeField, HideInInspector] private StringHash32[] m_HistoricalPopulationFactIds;

        [Header("Prediction")]
        [SerializeField] private uint m_PredictionTicks = 0;
        [SerializeField] private ActorCountRange[] m_TargetActors = null;
        [SerializeField, KeyValuePair("Id", "Population")] private ActorCountI32[] m_AdjustableActors = null;

        [Header("Water Properties")]
        [SerializeField] private bool m_DisplayWaterProperties = false;
        [SerializeField, ShowIfField("m_DisplayWaterProperties")] private DuplicatedWaterPropertyId m_PropertyToDisplay = DuplicatedWaterPropertyId.Light;

        [Header("Labels")]
        [SerializeField] private TextId m_TitleId = default;
        [SerializeField] private TextId m_DescId = default;
        [SerializeField] private TextId m_TickLabelId = default;

        [Header("Results")]
        [SerializeField, FactId(typeof(BFModel))] private SerializedHash32 m_BestiaryModelId = null;

        #endregion // Inspector

        [SerializeField, HideInInspector] private List<BestiaryDesc> m_Critters;

        public uint Seed() { return m_Seed; }

        public BestiaryDesc Environment() { return m_Environment; }
        public ListSlice<BestiaryDesc> Critters() { return m_Critters; }
        public ListSlice<ActorCountU32> Actors() { return m_InitialActors; }
        public ListSlice<StringHash32> PopulationHistoryFacts() { return m_HistoricalPopulationFactIds; }

        public uint TickCount() { return m_TickCount; }
        public int TickScale() { return m_TickScale; }

        public uint PredictionTicks() { return m_PredictionTicks; }
        public ListSlice<ActorCountRange> PredictionTargets() { return m_TargetActors; }
        public ListSlice<ActorCountI32> AdjustableActors() { return m_AdjustableActors; }

        public bool DisplayWaterProperties() { return m_DisplayWaterProperties; }
        public WaterPropertyId WaterProperty() { return m_DisplayWaterProperties ? (WaterPropertyId) m_PropertyToDisplay : WaterPropertyId.NONE; }

        public TextId TitleId() { return m_TitleId; }
        public TextId DescId() { return m_DescId; }
        public TextId TickLabelId() { return m_TickLabelId; }

        public StringHash32 BestiaryModelId() { return m_BestiaryModelId; }
        
        public uint TotalTicks() { return m_TickCount + m_PredictionTicks; }

        public bool IsInHistorical(StringHash32 inId)
        {
            foreach(var actorCount in m_InitialActors)
            {
                if (actorCount.Id == inId)
                    return true;
            }

            return false;
        }
    }
}