using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Water Property History")]
    public class BFWaterPropertyHistory : BFBase
    {
        #region Inspector

        [Header("Property")]
        [AutoEnum] public WaterPropertyId Property = WaterPropertyId.Temperature;
        [AutoEnum] public BFGraphType Graph = BFGraphType.Flat;

        #endregion // Inspector

        private BFWaterPropertyHistory() : base(BFTypeId.WaterPropertyHistory) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.WaterPropertyHistory, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.WaterPropertyHistory, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.WaterPropertyHistory, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return WaterPropertyDB.SortByVisualOrder(((BFWaterPropertyHistory) x).Property, ((BFWaterPropertyHistory) y).Property);
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFWaterPropertyHistory propFact = (BFWaterPropertyHistory) inFact;
            WaterPropertyDesc desc = BestiaryUtils.Property(propFact.Property);
            return Loc.Format(desc.EnvironmentHistoryFactFormat(), propFact.Parent.CommonName(), BestiaryUtils.GraphTypeToTextId(propFact.Graph), propFact.Parent.HistoricalRecordDuration());
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFWaterPropertyHistory propFact = (BFWaterPropertyHistory) inFact;
            return BestiaryUtils.Property(propFact.Property).Icon();
        }

        #endregion // Behavior
    }
}