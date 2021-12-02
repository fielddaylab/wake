using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Simulation")]
    public class BFSim : BFBase
    {
        #region Inspector

        [Header("Sync")]

        [KeyValuePair("Id", "Population")] public ActorCountU32[] InitialActors = null;
        public WaterPropertyBlockF32 InitialWater = default;
        public uint SyncTickCount = 10;

        [Header("Predict")]

        public uint PredictTickCount = 10;

        [Header("Resources")]

        public float OxygenPerTick = 4;
        public float CarbonDioxidePerTick = 4;

        #endregion // Inspector

        private BFSim() : base(BFTypeId.Sim) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Sim, BFShapeId.None, 0, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Sim, null, null, null);
            BFType.DefineEditor(BFTypeId.Sim, null, BFMode.Internal);
        }

        #endregion // Behavior
    }
}