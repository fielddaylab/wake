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
        [FormerlySerializedAs("m_StartingMass"), FormerlySerializedAs("m_MassPerPopulation")] public uint MassPerPopulation = 0;
        [FormerlySerializedAs("m_ScarcityLevel")] public uint ScarcityLevel = 10000;
        
        [Header("Display")]
        [FormerlySerializedAs("m_MassDisplayScale")] public float MassDisplayScale = 1;
        [FormerlySerializedAs("m_PopulationSoftCap")] public uint PopulationSoftCap = 1000;
        [FormerlySerializedAs("m_PopulationSoftIncrement")] public uint PopulationSoftIncrement = 1;
        [FormerlySerializedAs("m_PopulationHardCap")] public uint PopulationHardCap = 1000;

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