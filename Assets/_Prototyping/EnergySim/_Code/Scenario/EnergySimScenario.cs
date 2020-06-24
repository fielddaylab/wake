using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Scenario")]
    public class EnergySimScenario : ScriptableObject, IEnergySimScenario
    {
        #region Inspector

        [Header("Environment")]

        [SerializeField, EnvironmentTypeId]
        private FourCC m_EnvType = FourCC.Zero;

        [SerializeField]
        private VarPair[] m_InitialResources = null;

        [SerializeField]
        private VarPairF[] m_InitialProperties = null;

        [Header("Actors")]

        [SerializeField]
        private ActorCount[] m_InitialActors = null;

        [Header("Ticks")]

        [SerializeField]
        private int m_TickActionCount = 8;

        [SerializeField]
        private int m_TickScale = 1;

        [SerializeField]
        private ushort m_Duration = 60;

        #endregion // Inspector

        #region IEnergySimScenario

        public void Initialize(EnergySimState ioState, EnergySimDatabase inDatabase)
        {
            ioState.Environment.Type = m_EnvType;

            for(int i = 0; i < m_InitialActors.Length; ++i)
            {
                ActorType type = inDatabase.ActorType(m_InitialActors[i].Id);
                ioState.AddActors(type, (int) m_InitialActors[i].Count);
            }

            for(int i = 0; i < m_InitialResources.Length; ++i)
            {
                ioState.AddResourceToEnvironment(inDatabase, m_InitialResources[i].Id, (ushort) m_InitialResources[i].Value);
            }

            for(int i = 0; i < m_InitialProperties.Length; ++i)
            {
                ioState.SetPropertyInEnvironment(inDatabase, m_InitialProperties[i].Id, m_InitialProperties[i].Value);
            }
        }

        public int TickActionCount()
        {
            return m_TickActionCount;
        }

        public int TickScale()
        {
            return m_TickScale;
        }

        public ushort TotalTicks()
        {
            return m_Duration;
        }

        public bool TryCalculateProperty(FourCC inPropertyId, IEnergySimStateReader inReader, EnergySimDatabase inDatabase, out float outValue)
        {
            // throw new NotImplementedException();

            outValue = default(float);
            return false;
        }

        #endregion // IEnergySimScenario
    }
}