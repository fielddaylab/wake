using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Population")]
    public class BFPopulation : BFBase
    {
        #region Inspector

        [Header("Population")]
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Critter = null;
        public uint Value = 0;

        #endregion // Inspector

        private BFPopulation() : base(BFTypeId.Population) { }

        #region Behavior

        static private readonly TextId PopulationSentence = "factFormat.population";
        static private readonly TextId PopulationHerdSentence = "factFormat.population.herd";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Population, BFShapeId.Population, 0, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Population, null, GenerateDetails, null, (f) => ((BFPopulation) f).Critter, null);
            BFType.DefineEditor(BFTypeId.Population, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return BestiaryDesc.SortById(((BFPopulation) x).Critter, ((BFPopulation) y).Critter);
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFPopulation fact = (BFPopulation) inFact;
            BFDetails details;
            details.Header = Loc.Find("fact.population.header");
            details.Image = fact.Critter.ImageSet();

            TextId format = PopulationSentence;
            if (fact.Critter.HasFlags(BestiaryDescFlags.TreatAsHerd))
                format = PopulationHerdSentence;
            details.Description = Loc.Format(format, BestiaryUtils.FormatPopulation(fact.Critter, fact.Value), fact.Critter.PluralCommonName(), BestiaryUtils.LocationLabel(fact.Parent));

            return details;
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFPopulation fact = (BFPopulation) inFact;
            return fact.Critter.Icon();
        }

        #endregion // Behavior
    }
}