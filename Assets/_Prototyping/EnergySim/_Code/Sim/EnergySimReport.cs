using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class EnergySimReport
    {
        private EnergySimState m_Source;

        public VarPair[] EnvResources;
        public VarPairF[] EnvProperties;

        public ActorCount[] ActorCounts;
        public ActorCount[] ActorMasses;
    }
}