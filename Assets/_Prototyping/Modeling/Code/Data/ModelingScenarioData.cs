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
        [SerializeField] private ActorCount[] m_InitialActors = null;
        [SerializeField] private int m_TickCount = 1000;
        [SerializeField] private int m_TickIncrement = 10;

        #endregion // Inspector

        public BestiaryDesc Environment() { return m_Environment; }
        public IReadOnlyList<BFBase> Facts() { return m_Facts; }
        public IReadOnlyList<ActorCount> Actors() { return m_InitialActors; }

        public int TickCount() { return m_TickCount; }
        public int TickIncrement() { return m_TickIncrement; }
    }
}