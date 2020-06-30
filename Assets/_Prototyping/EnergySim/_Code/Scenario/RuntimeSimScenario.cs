using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class RuntimeSimScenario : IEnergySimScenario
    {
        // TODO: Improve access pattern

        public FourCC EnvType = FourCC.Zero;
        public VarPair[] InitialResources = null;
        public VarPairF[] InitialProperties = null;
        public ActorCount[] InitialActors = null;
        public int TickActionCount = 8;
        public int TickScale = 1;
        public ushort Duration = 60;

        private int m_Version;

        #region IEnergySimScenario

        public void Initialize(EnergySimState ioState, ISimDatabase inDatabase)
        {
            ioState.Environment.Type = EnvType;

            for(int i = 0; i < InitialActors.Length; ++i)
            {
                ActorType type = inDatabase.Actors.Get(InitialActors[i].Id);
                ioState.AddActors(type, (int) InitialActors[i].Count);
            }

            for(int i = 0; i < InitialResources.Length; ++i)
            {
                ioState.AddResourceToEnvironment(inDatabase, InitialResources[i].Id, (ushort) InitialResources[i].Value);
            }

            for(int i = 0; i < InitialProperties.Length; ++i)
            {
                ioState.SetPropertyInEnvironment(inDatabase, InitialProperties[i].Id, InitialProperties[i].Value);
            }
        }

        int IEnergySimScenario.TickActionCount()
        {
            return TickActionCount;
        }

        int IEnergySimScenario.TickScale()
        {
            return TickScale;
        }

        public ushort TotalTicks()
        {
            return Duration;
        }

        public bool TryCalculateProperty(FourCC inPropertyId, IEnergySimStateReader inReader, ISimDatabase inDatabase, out float outValue)
        {
            // throw new NotImplementedException();

            outValue = default(float);
            return false;
        }

        #endregion // IEnergySimScenario

        #region IUpdateVersion

        [UnityEngine.Scripting.Preserve]
        int IUpdateVersioned.GetUpdateVersion()
        {
            return m_Version;
        }

        public void Dirty()
        {
            UpdateVersion.Increment(ref m_Version);
        }

        #endregion // IUpdateVersion
    }
}