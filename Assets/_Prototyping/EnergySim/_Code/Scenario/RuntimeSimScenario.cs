using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class RuntimeSimScenario : IEnergySimScenario, ISerializedObject, ISerializedVersion
    {
        // TODO: Improve access pattern

        public FourCC EnvType = FourCC.Zero;
        public VarPair[] InitialResources = null;
        public VarPairF[] InitialProperties = null;
        public ActorCount[] InitialActors = null;
        public int TickActionCount = 8;
        public int TickScale = 1;
        public ushort Duration = 60;
        public ushort Seed = 0;

        private int m_Version;

        #region IEnergySimScenario

        public void Initialize(EnergySimState ioState, ISimDatabase inDatabase, System.Random inRandom)
        {
            ioState.Environment.Type = EnvType;

            for(int i = 0; i < InitialActors.Length; ++i)
            {
                ActorType type = inDatabase.Actors[InitialActors[i].Id];
                ioState.AddActors(type, (int) InitialActors[i].Count, inRandom);
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

        ushort IEnergySimScenario.Seed()
        {
            return Seed;
        }

        public ushort TotalTicks()
        {
            return Duration;
        }

        public bool TryCalculateProperty(FourCC inPropertyId, IEnergySimStateReader inReader, ISimDatabase inDatabase, System.Random inRandom, out float outValue)
        {
            // throw new NotImplementedException();

            outValue = default(float);
            return false;
        }

        public IEnumerable<FourCC> StartingActorIds()
        {
            foreach(var actorPair in InitialActors)
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

        public void Dirty()
        {
            UpdateVersion.Increment(ref m_Version);
        }

        #endregion // IUpdateVersion

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 2; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("envType", ref EnvType);
            ioSerializer.ObjectArray("resources", ref InitialResources);
            ioSerializer.ObjectArray("properties", ref InitialProperties);
            ioSerializer.ObjectArray("actors", ref InitialActors);
            ioSerializer.Serialize("tickActionCount", ref TickActionCount);
            ioSerializer.Serialize("tickScale", ref TickScale);
            ioSerializer.Serialize("duration", ref Duration);

            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Serialize("seed", ref Seed);
            }
        }

        #endregion // ISerializedObject
    }
}