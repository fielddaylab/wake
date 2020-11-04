using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Scenario")]
    public class EnergySimScenario : ScriptableObject, IEnergySimScenario
    {
        #region Inspector

        [SerializeField]
        private ScenarioPackageHeader m_Header = null;

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

        [SerializeField]
        private ushort m_Seed = 0;

        #endregion // Inspector

        [NonSerialized] private int m_Version;

        public ScenarioPackage CreateRuntimePackage()
        {
            RuntimeSimScenario runtime = new RuntimeSimScenario();
            runtime.EnvType = m_EnvType;
            runtime.InitialResources = (VarPair[]) m_InitialResources.Clone();
            runtime.InitialProperties = (VarPairF[]) m_InitialProperties.Clone();
            runtime.InitialActors = (ActorCount[]) m_InitialActors.Clone();
            runtime.TickActionCount = m_TickActionCount;
            runtime.TickScale = m_TickScale;
            runtime.Duration = m_Duration;
            runtime.Seed = m_Seed;

            ScenarioPackageHeader header = new ScenarioPackageHeader();
            header.Id = m_Header.Id;
            header.LastUpdated = m_Header.LastUpdated;
            header.DatabaseId = m_Header.DatabaseId;
            header.Name = m_Header.Name;
            header.Author = m_Header.Author;
            header.Description = m_Header.Description;
            header.ContentAreas = m_Header.ContentAreas;
            header.Qualitative = m_Header.Qualitative;
            header.PartnerIntroQuote = m_Header.PartnerIntroQuote;
            header.PartnerHelpQuote = m_Header.PartnerHelpQuote;
            header.PartnerCompleteQuote = m_Header.PartnerCompleteQuote;
            header.SuccessThreshold = m_Header.SuccessThreshold;

            ScenarioPackage package = new ScenarioPackage();
            package.Header = header;
            package.Data = runtime;
            return package;
        }

        #region IEnergySimScenario

        public string Id() { return m_Header.Id; }

        public void Initialize(EnergySimState ioState, ISimDatabase inDatabase, System.Random inRandom)
        {
            ioState.Environment.Type = m_EnvType;

            for(int i = 0; i < m_InitialActors.Length; ++i)
            {
                ActorType type = inDatabase.Actors[m_InitialActors[i].Id];
                ioState.AddActors(type, (int) m_InitialActors[i].Count, inRandom);
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

        public ushort Seed()
        {
            return m_Seed;
        }

        public bool TryCalculateProperty(FourCC inPropertyId, IEnergySimStateReader inReader, ISimDatabase inDatabase, System.Random inRandom, out float outValue)
        {
            // throw new NotImplementedException();

            outValue = default(float);
            return false;
        }

        public IEnumerable<FourCC> StartingActorIds()
        {
            foreach(var actorPair in m_InitialActors)
            {
                if (actorPair.Count > 0)
                {
                    yield return actorPair.Id;
                }
            }
        }

        #endregion // IEnergySimScenario

        #region IUpdateVersion

        [UnityEngine.Scripting.Preserve]
        int IUpdateVersioned.GetUpdateVersion()
        {
            return m_Version;
        }

        #endregion // IUpdateVersion

        #region Unity Events

        #if UNITY_EDITOR

        private void OnValidate()
        {
            UpdateVersion.Increment(ref m_Version);
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    }
}