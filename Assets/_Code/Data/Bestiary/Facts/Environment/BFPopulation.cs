using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Population")]
    public class BFPopulation : BFBase
    {
        #region Inspector

        [Header("Population")]
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Critter = null;
        public uint Value = 0;
        public byte SiteVersion = 0;

        #endregion // Inspector

        private BFPopulation() : base(BFTypeId.Population) { }

        #region Behavior

        static private readonly TextId PopulationSentence = "factFormat.population";
        static private readonly TextId PopulationHerdSentence = "factFormat.population.herd";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Population, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Population, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.Population, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return BestiaryDesc.SortById(((BFPopulation) x).Critter, ((BFPopulation) y).Critter);
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFPopulation fact = (BFPopulation) inFact;
            TextId format = PopulationSentence;
            if (fact.Critter.HasFlags(BestiaryDescFlags.TreatAsHerd))
                format = PopulationHerdSentence;
            return Loc.Format(format, BestiaryUtils.FormatPopulation(fact.Parent, fact.Value), fact.Critter.PluralCommonName(), fact.Parent.CommonName());
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFPopulation fact = (BFPopulation) inFact;
            return fact.Critter.Icon();
        }

        #endregion // Behavior
    }
}