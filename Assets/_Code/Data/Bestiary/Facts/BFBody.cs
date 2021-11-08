using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Body")]
    public class BFBody : BFBase
    {
        #region Inspector

        [Header("Body")]
        public uint MassPerPopulation = 0;
        [Tooltip("When total mass sinks below this threshold, critters will die and be eaten more slowly")] public uint ScarcityLevel = 10000;
        
        [Header("Display")]
        public float MassDisplayScale = 1;
        [Tooltip("Population cap for player adjustments in modeling")] public uint PopulationSoftCap = 1000;
        [Tooltip("Population increment for player adjustments in modeling")] public uint PopulationSoftIncrement = 1;
        [Tooltip("Absolute maximum population")] public uint PopulationHardCap = 1000;

        #endregion // Inspector

        private BFBody() : base(BFTypeId.Body) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Body, BFShapeId.None, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Body, null, null, null);
            BFType.DefineEditor(BFTypeId.Body, null, BFMode.Internal);
        }

        #endregion // Behavior
    }
}