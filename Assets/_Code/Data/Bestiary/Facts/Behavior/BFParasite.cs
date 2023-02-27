using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Parasites")]
    public class BFParasite : BFBehavior
    {
        #region Inspector

        [Header("Parasite")]
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Critter = null;
        public float Ratio = 0;

        #endregion // Inspector

        private BFParasite() : base(BFTypeId.Parasite) { }

        #region Behavior

        static public readonly TextId StressesVerb = "words.stresses";
        static private readonly TextId IsStressedByVerb = "words.isStressedBy";
        static private readonly TextId StressesSentence = "factFormat.stresses";
        static private readonly TextId StressesRateSentence = "factFormat.stresses.rate";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Parasite, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.IsGraphable | BFFlags.HasRate, BFDiscoveredFlags.Base, null);
            BFType.DefineMethods(BFTypeId.Parasite, CollectReferences, GenerateDetails, GenerateFragments, (f) => ((BFParasite)f).Critter, null);
            BFType.DefineEditor(BFTypeId.Parasite, null, BFMode.Player);
        }

        static private void CollectReferences(BFBase inFact, HashSet<StringHash32> outCritters)
        {
            BFParasite fact = (BFParasite) inFact;
            outCritters.Add(fact.Critter.Id());
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFParasite fact = (BFParasite) inFact;
            
            if (BFType.IsBorrowed(inFact, inReference))
            {
                if (BFType.HasRate(inFlags))
                {
                    yield return BFFragment.CreateAmount(fact.Ratio);
                }
                yield return BFFragment.CreateLocNoun(fact.Critter.CommonName());
                yield return BFFragment.CreateLocVerb(IsStressedByVerb);
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            }
            else
            {
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
                yield return BFFragment.CreateLocVerb(StressesVerb);
                if (BFType.HasRate(inFlags))
                {
                    yield return BFFragment.CreateAmount(fact.Ratio);
                }
                yield return BFFragment.CreateLocNoun(fact.Critter.CommonName());
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFParasite fact = (BFParasite) inFact;

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Critter.ImageSet();
            if (BFType.HasRate(inFlags))
            {
                details.Description = Loc.Format(StressesRateSentence, inFact.Parent.CommonName(), fact.Ratio, fact.Critter.CommonName());
            }
            else
            {
                details.Description = Loc.Format(StressesSentence, inFact.Parent.CommonName(), fact.Critter.CommonName());
            }

            return details;
        }

        #endregion // Behavior
    }
}