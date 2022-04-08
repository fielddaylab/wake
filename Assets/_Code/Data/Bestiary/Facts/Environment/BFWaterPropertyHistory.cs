using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Water Property History")]
    public class BFWaterPropertyHistory : BFBase
    {
        #region Inspector

        [Header("Property History")]
        [AutoEnum] public WaterPropertyId Property = WaterPropertyId.Temperature;
        [AutoEnum] public BFGraphType Graph = BFGraphType.Flat;

        #endregion // Inspector

        private BFWaterPropertyHistory() : base(BFTypeId.WaterPropertyHistory) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.WaterPropertyHistory, BFShapeId.WaterPropertyHistory, BFFlags.HideFactInDetails | BFFlags.EnvironmentFact, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.WaterPropertyHistory, null, GenerateDetails, null, null, (f) => ((BFWaterPropertyHistory) f).Property);
            BFType.DefineEditor(BFTypeId.WaterPropertyHistory, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return WaterPropertyDB.SortByVisualOrder(((BFWaterPropertyHistory) x).Property, ((BFWaterPropertyHistory) y).Property);
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFWaterPropertyHistory fact = (BFWaterPropertyHistory) inFact;
            WaterPropertyDesc desc = BestiaryUtils.Property(fact.Property);

            BFDetails details;
            details.Header = Loc.Find("fact.waterChemHistory.header");
            details.Image = Services.Assets.Bestiary.GraphTypeToImage(fact.Graph);
            details.Description = Loc.Format(desc.EnvironmentHistoryFactFormat(), BestiaryUtils.LocationLabel(fact.Parent), BestiaryUtils.GraphTypeToTextId(fact.Graph), fact.Parent.HistoricalRecordDuration());

            return details;
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFWaterPropertyHistory propFact = (BFWaterPropertyHistory) inFact;
            return BestiaryUtils.Property(propFact.Property).Icon();
        }

        #endregion // Behavior
    }
}