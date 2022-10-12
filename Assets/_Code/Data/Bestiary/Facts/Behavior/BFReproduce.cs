using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Reproduce")]
    public class BFReproduce : BFBehavior
    {
        #region Inspector

        [Header("Reproduction")]
        [Range(0, 5)] public float Amount = 0;

        #endregion // Inspector

        private BFReproduce() : base(BFTypeId.Reproduce) { }

        #region Behavior

        static public readonly TextId ReproduceVerb = "words.reproduce";
        static public readonly TextId ReproduceDisabledVerb = "words.reproduce.disabled";
        static private readonly TextId ReproduceSentence = "factFormat.reproduce";
        static private readonly TextId ReproduceDisabledSentence = "factFormat.reproduce.disabled";
        static private readonly TextId ReproduceSentenceStressed = "factFormat.reproduce.stressed";
        static private readonly TextId ReproduceDisabledSentenceStressed = "factFormat.reproduce.stressed.disabled";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Reproduce, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.SelfTarget | BFFlags.HasRate, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Reproduce, null, GenerateDetails, GenerateFragments, null, null);
            BFType.DefineEditor(BFTypeId.Reproduce, null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFReproduce fact = (BFReproduce) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            if (fact.Amount == 0)
            {
                yield return BFFragment.CreateLocVerb(ReproduceDisabledVerb);
            }
            else
            {
                yield return BFFragment.CreateLocVerb(ReproduceVerb);
                yield return BFFragment.CreateAmount(BestiaryUtils.FormatPercentage(fact.Amount));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFReproduce fact = (BFReproduce) inFact;
            
            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Parent.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                if (fact.Amount == 0)
                {
                    details.Description = Loc.Format(ReproduceDisabledSentenceStressed, inFact.Parent.CommonName());
                }
                else
                {
                    details.Description = Loc.Format(ReproduceSentenceStressed, inFact.Parent.CommonName(), BestiaryUtils.FormatPercentage(fact.Amount));
                }
            }
            else
            {
                if (fact.Amount == 0)
                {
                    details.Description = Loc.Format(ReproduceDisabledSentence, inFact.Parent.CommonName());
                }
                else
                {
                    details.Description = Loc.Format(ReproduceSentence, inFact.Parent.CommonName(), BestiaryUtils.FormatPercentage(fact.Amount));
                }
            }

            return details;
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Bake(BakeFlags flags, BakeContext context)
        {
            bool bChanged = false;
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFReproduce>();
                if (pair != null)
                {
                    float compare = Amount - pair.Amount;
                    bChanged |= Ref.Replace(ref PairId, pair.Id);
                }
            }
            else
            {
                bChanged |= Ref.Replace(ref PairId, null);
            }
            return bChanged;
        }

        #endif // UNITY_EDITOR
    }
}