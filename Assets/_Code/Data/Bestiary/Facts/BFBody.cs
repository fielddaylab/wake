using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Body")]
    public class BFBody : BFBase
    {
        #region Inspector

        [Header("Body")]
        public uint MassPerPopulation = 0;
        public uint ScarcityLevel = 10000;
        
        [Header("Display")]
        public float MassDisplayScale = 1;
        public uint PopulationSoftCap = 1000;
        public uint PopulationSoftIncrement = 1;
        public uint PopulationHardCap = 1000;

        #endregion // Inspector

        private BFBody() : base(BFTypeId.Body) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Body, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Body, null, null, null);
            BFType.DefineEditor(BFTypeId.Body, (Sprite) null, BFMode.Internal);
        }

        #endregion // Behavior
    }
}