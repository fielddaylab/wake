using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    [CreateAssetMenu(menuName = "Aqualab/Modeling/Scenario Data", fileName = "NewModelingScenario")]
    public class ModelingScenarioData : ScriptableObject
    {
        #region Inspector

        [SerializeField] private BestiaryDesc m_Environment = null;
        [SerializeField] private BFBase[] m_Facts = null;
        
        [Header("Historical Data")]
        [SerializeField] private ActorCount[] m_InitialActors = null;
        [SerializeField] private int m_TickCount = 1000;
        [SerializeField] private int m_TickScale = 10;

        [Header("Prediction")]
        [SerializeField] private int m_PredictionTicks = 0;
        // TODO: prediction targets?

        #endregion // Inspector

        [NonSerialized] private bool m_Optimized;

        public BestiaryDesc Environment() { return m_Environment; }
        public IReadOnlyList<BFBase> Facts() { Optimize(); return m_Facts; }
        public IReadOnlyList<ActorCount> Actors() { Optimize(); return m_InitialActors; }

        public int TickCount() { return m_TickCount; }
        public int TickScale() { return m_TickScale; }

        public int PredictionTicks() { return m_PredictionTicks; }

        private void Optimize()
        {
            if (m_Optimized)
                return;

            KeyValueUtils.SortByKey<StringHash32, uint, ActorCount>(m_InitialActors);
            m_Optimized = true;
        }
    }
}