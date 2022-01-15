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
            BFType.DefineAttributes(BFTypeId.WaterProperty, BFShapeId.WaterProperty, 0, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.WaterProperty, null, GenerateDetails, null, null, (f) => ((BFWaterProperty) f).Property);
            BFType.DefineEditor(BFTypeId.WaterProperty, DefaultIcon, BFMode.Always);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return WaterPropertyDB.SortByVisualOrder(((BFWaterProperty) x).Property, ((BFWaterProperty) y).Property);
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFWaterProperty fact = (BFWaterProperty) inFact;
            WaterPropertyDesc desc = BestiaryUtils.Property(fact.Property);
            
            BFDetails details;
            details.Header = Loc.Find("fact.waterChem.header");
            details.Image = desc.ImageSet();
            details.Description = Loc.Format(desc.EnvironmentFactFormat(), BestiaryUtils.LocationLabel(fact.Parent), desc.FormatValue(fact.Value));

            return details;
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFWaterProperty propFact = (BFWaterProperty) inFact;
            return BestiaryUtils.Property(propFact.Property).Icon();
        }

        #endregion // Behavior
    }
}