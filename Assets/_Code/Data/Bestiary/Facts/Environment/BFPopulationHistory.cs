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
            BFType.DefineAttributes(BFTypeId.PopulationHistory, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.PopulationHistory, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.PopulationHistory, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return BestiaryDesc.SortById(((BFPopulationHistory) x).Critter, ((BFPopulationHistory) y).Critter);
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFPopulationHistory fact = (BFPopulationHistory) inFact;
            return Loc.Format(PopulationSentence, fact.Critter.CommonName(), fact.Parent.CommonName(), BestiaryUtils.GraphTypeToTextId(fact.Graph), fact.Parent.HistoricalRecordDuration());
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFPopulationHistory fact = (BFPopulationHistory) inFact;
            return fact.Critter.Icon();
        }

        #endregion // Behavior
    }
}