using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Water Property")]
    public class BFWaterProperty : BFBase
    {
        #region Inspector

        [Header("Property")]
        [AutoEnum] public WaterPropertyId Property = WaterPropertyId.Temperature;
        public float Value = 0;

        #endregion // Inspector

        private BFWaterProperty() : base(BFTypeId.WaterProperty) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.WaterProperty, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.WaterProperty, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.WaterProperty, DefaultIcon, BFMode.Always);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return WaterPropertyDB.SortByVisualOrder(((BFWaterProperty) x).Property, ((BFWaterProperty) y).Property);
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFWaterProperty propFact = (BFWaterProperty) inFact;
            WaterPropertyDesc desc = BestiaryUtils.Property(propFact.Property);
            return Loc.Format(desc.EnvironmentFactFormat(), propFact.Parent.CommonName(), desc.FormatValue(propFact.Value));
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFWaterProperty propFact = (BFWaterProperty) inFact;
            return BestiaryUtils.Property(propFact.Property).Icon();
        }

        #endregion // Behavior
    }
}