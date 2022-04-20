using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Eats")]
    public class BFEat : BFBehavior
    {
        #region Inspector

        [Header("Eating")]
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Critter = null;
        public float Amount = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFEat() : base(BFTypeId.Eat) { }

        #region Behavior

        static public readonly TextId EatVerb = "words.eat";
        static private readonly TextId IsEatenByVerb = "words.isEatenBy";
        static public readonly TextId CatchVerb = "words.catch";
        static private readonly TextId IsCaughtByVerb = "words.isCaughtBy";
        static private readonly TextId EatSentence = "factFormat.eat";
        static private readonly TextId EatRateSentence = "factFormat.eat.rate";
        static private readonly TextId CatchSentence = "factFormat.catch";
        static private readonly TextId EatSentenceStressed = "factFormat.eat.stressed";
        static private readonly TextId EatRateSentenceStressed = "factFormat.eat.stressed.rate";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Eat, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.IsGraphable, BFDiscoveredFlags.Base, Compare);
            BFType.DefineMethods(BFTypeId.Eat, CollectReferences, GenerateDetails, GenerateFragments, (f) => ((BFEat)f).Critter, null);
            BFType.DefineEditor(BFTypeId.Eat, null, BFMode.Player);
        }

        static private void CollectReferences(BFBase inFact, HashSet<StringHash32> outCritters)
        {
            BFEat fact = (BFEat) inFact;
            outCritters.Add(fact.Critter.Id());
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFEat fact = (BFEat) inFact;
            bool bIsHuman = inFact.Parent.HasFlags(BestiaryDescFlags.Human);

            if (BFType.IsBorrowed(inFact, inReference))
            {
                if (BFType.HasRate(inFlags))
                {
                    yield return BFFragment.CreateAmount(BestiaryUtils.FormatMass(fact.Amount));
                }
                yield return BFFragment.CreateLocNoun(fact.Critter.CommonName());
                yield return BFFragment.CreateLocVerb(bIsHuman ? IsCaughtByVerb : IsEatenByVerb);
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            }
            else
            {
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
                yield return BFFragment.CreateLocVerb(bIsHuman ? CatchVerb : EatVerb);
                if (BFType.HasRate(inFlags))
                {
                    yield return BFFragment.CreateAmount(BestiaryUtils.FormatMass(fact.Amount));
                }
                yield return BFFragment.CreateLocNoun(fact.Critter.CommonName());
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFEat fact = (BFEat) inFact;
            bool bIsHuman = inFact.Parent.HasFlags(BestiaryDescFlags.Human);

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Critter.ImageSet();

            if (bIsHuman)
            {
                details.Description = Loc.Format(CatchSentence,
                    fact.Parent.CommonName(),
                    BestiaryUtils.FormatMass(fact.Amount),
                    fact.Critter.CommonName());
            }
            else if (fact.OnlyWhenStressed)
            {
                if (BFType.HasRate(inFlags))
                {
                    details.Description = Loc.Format(EatRateSentenceStressed,
                        inFact.Parent.CommonName(),
                        BestiaryUtils.FormatMass(fact.Amount),
                        fact.Critter.CommonName());
                }
                else
                {
                    details.Description = Loc.Format(EatSentenceStressed,
                        inFact.Parent.CommonName(),
                        fact.Critter.CommonName());
                }
            }
            else if (BFType.HasRate(inFlags))
            {
                details.Description = Loc.Format(EatRateSentence,
                    inFact.Parent.CommonName(),
                    BestiaryUtils.FormatMass(fact.Amount),
                    fact.Critter.CommonName());
            }
            else
            {
                details.Description = Loc.Format(EatSentence,
                    inFact.Parent.CommonName(),
                    fact.Critter.CommonName());
            }

            return details;
        }

        static private int Compare(BFBase x, BFBase y)
        {
            int critterCompare = BestiaryDesc.SortById(((BFEat) x).Critter, ((BFEat) y).Critter);
            if (critterCompare != 0)
                return critterCompare;

            return CompareStressedPair(x, y);
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        protected override bool IsPair(BFBehavior inOther)
        {
            BFEat eat = inOther as BFEat;
            return eat != null && eat.Critter == Critter;
        }

        public override bool Bake(BakeFlags flags)
        {
            bool bChanged = false;
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFEat>();
                if (pair != null)
                {
                    float compare = Amount - pair.Amount;
                    bChanged |= Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Less, QualCompare.More, QualCompare.SameRate));
                    bChanged |= Ref.Replace(ref PairId, pair.Id);
                }
            }
            else
            {
                bChanged |= Ref.Replace(ref m_Relative, QualCompare.Null);
                bChanged |= Ref.Replace(ref PairId, null);
            }
            return bChanged;
        }

        #endif // UNITY_EDITOR
    }
}