using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Population History")]
    public class BFPopulationHistory : BFBase
    {
        #region Inspector

        [Header("Population History")]
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Critter = null;
        [AutoEnum] public BFGraphType Graph = BFGraphType.Flat;

        #endregion // Inspector

        private BFPopulationHistory() : base(BFTypeId.PopulationHistory) { }

        #region Behavior

        static private readonly TextId PopulationSentence = "factFormat.populationHistory";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.PopulationHistory, BFShapeId.PopulationHistory, BFFlags.HideFactInDetails | BFFlags.EnvironmentFact, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.PopulationHistory, null, GenerateDetails, null, (f) => ((BFPopulationHistory) f).Critter, null);
            BFType.DefineEditor(BFTypeId.PopulationHistory, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return BestiaryDesc.SortById(((BFPopulationHistory) x).Critter, ((BFPopulationHistory) y).Critter);
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFPopulationHistory fact = (BFPopulationHistory) inFact;
            BFDetails details;
            details.Header = Loc.Find("fact.populationHistory.header");
            details.Image = Services.Assets.Bestiary.GraphTypeToImage(fact.Graph);
            details.Description = Loc.Format(PopulationSentence, fact.Critter.CommonName(), BestiaryUtils.LocationLabel(fact.Parent), BestiaryUtils.GraphTypeToTextId(fact.Graph), fact.Parent.HistoricalRecordDuration());

            return details;
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFPopulationHistory fact = (BFPopulationHistory) inFact;
            return fact.Critter.Icon();
        }

        #endregion // Behavior
    }
}