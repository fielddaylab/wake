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
        #region Inspector

        [SerializeField] private BestiaryDesc m_Environment = null;
        [SerializeField] private BFBase[] m_Facts = null;
        
        [Header("Historical Data")]
        [SerializeField] private ActorCountU32[] m_InitialActors = null;
        [SerializeField] private uint m_TickCount = 1000;
        [SerializeField] private int m_TickScale = 10;

        [Header("Prediction")]
        [SerializeField] private uint m_PredictionTicks = 0;
        [SerializeField] private ActorCountU32[] m_TargetActors = null;
        [SerializeField] private ActorCountI32[] m_AdjustableActors = null;

        [Header("Labels")]
        [SerializeField] private SerializedHash32 m_TitleId = null;
        [SerializeField] private SerializedHash32 m_DescId = null;
        [SerializeField] private SerializedHash32 m_TickLabelId = null;

        [Header("Results")]
        [SerializeField] private SerializedHash32 m_CompletedItem = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Optimized;
        [NonSerialized] private List<BestiaryDesc> m_Critters;
        [NonSerialized] private HashSet<StringHash32> m_AllowedLines;

        public BestiaryDesc Environment() { return m_Environment; }
        public ListSlice<BestiaryDesc> Critters() { Optimize(); return m_Critters; }
        public ListSlice<BFBase> Facts() { Optimize(); return m_Facts; }
        public ListSlice<ActorCountU32> Actors() { Optimize(); return m_InitialActors; }

        public uint TickCount() { return m_TickCount; }
        public int TickScale() { return m_TickScale; }

        public uint PredictionTicks() { return m_PredictionTicks; }
        public ListSlice<ActorCountU32> PredictionTargets() { return m_TargetActors; }
        public ListSlice<ActorCountI32> AdjustableActors() { return m_AdjustableActors; }

        public StringHash32 TitleId() { return m_TitleId; }
        public StringHash32 DescId() { return m_DescId; }
        public StringHash32 TickLabelId() { return m_TickLabelId; }

        public StringHash32 CompletedItem() { return m_CompletedItem; }
        
        public uint TotalTicks() { return m_TickCount + m_PredictionTicks; }

        public bool ShouldGraph(StringHash32 inId)
        {
            return m_AllowedLines.Contains(inId);
        }

        private void Optimize()
        {
            if (m_Optimized)
                return;

            m_AllowedLines = new HashSet<StringHash32>();
            m_Critters = new List<BestiaryDesc>(m_InitialActors.Length);
            for(int i = 0; i < m_InitialActors.Length; ++i)
            {
                m_AllowedLines.Add(m_InitialActors[i].Id);
                m_Critters.Add(Services.Assets.Bestiary.Get(m_InitialActors[i].Id));
            }

            KeyValueUtils.SortByKey<StringHash32, uint, ActorCountU32>(m_InitialActors);
            KeyValueUtils.SortByKey<StringHash32, uint, ActorCountU32>(m_TargetActors);
            m_Optimized = true;
        }
    }
}