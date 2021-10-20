using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Eats")]
    public class BFEat : BFBehavior
    {
        #region Inspector

        [Header("Eating")]
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Critter = null;
        public uint Amount = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFEat() : base(BFTypeId.Eat) { }

        #region Behavior

        static public readonly TextId EatVerb = "words.eat";
        static private readonly TextId IsEatenByVerb = "words.isEatenBy";
        static public readonly TextId CatchVerb = "words.catch";
        static private readonly TextId IsCaughtByVerb = "words.isCaughtBy";
        static private readonly TextId EatSentence = "factFormat.eat";
        static private readonly TextId CatchSentence = "factFormat.catch";
        static private readonly TextId EatSentenceStressed = "factFormat.eat.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Eat, BFDiscoveredFlags.Base, Compare);
            BFType.DefineMethods(BFTypeId.Eat, CollectReferences, GenerateSentence, GenerateFragments);
            BFType.DefineEditor(BFTypeId.Eat, DefaultIcon, BFMode.Player);
        }

        static private void CollectReferences(BFBase inFact, HashSet<StringHash32> outCritters)
        {
            BFEat fact = (BFEat) inFact;
            outCritters.Add(fact.Critter.Id());
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            BFEat fact = (BFEat) inFact;
            bool bIsHuman = inFact.Parent.HasFlags(BestiaryDescFlags.Human);

            if (inReference == null || inReference == fact.Parent)
            {
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
                yield return BFFragment.CreateLocVerb(bIsHuman ? CatchVerb : EatVerb);
                if (fact.OnlyWhenStressed)
                {
                    yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
                }
                if ((inFlags & BFDiscoveredFlags.Rate) != 0)
                {
                    yield return BFFragment.CreateAdjective(string.Format("({0})", BestiaryUtils.Property(WaterPropertyId.Mass).FormatValue(fact.Amount)));
                }
                yield return BFFragment.CreateLocNoun(fact.Critter.CommonName());
            }
            else
            {
                yield return BFFragment.CreateLocNoun(fact.Critter.CommonName());
                yield return BFFragment.CreateLocVerb(bIsHuman ? IsCaughtByVerb : IsEatenByVerb);
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
                if (fact.OnlyWhenStressed)
                {
                    yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
                }
                if ((inFlags & BFDiscoveredFlags.Rate) != 0)
                {
                    yield return BFFragment.CreateAdjective(string.Format("({0})", BestiaryUtils.Property(WaterPropertyId.Mass).FormatValue(fact.Amount)));
                }
            }
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFEat fact = (BFEat) inFact;
            bool bIsHuman = inFact.Parent.HasFlags(BestiaryDescFlags.Human);

            if (bIsHuman)
            {
                return Loc.Format(CatchSentence, fact.Parent.CommonName(), fact.Critter.CommonName());
            }

            if (fact.OnlyWhenStressed)
            {
                return Loc.Format(EatSentenceStressed, inFact.Parent.CommonName(), QualitativeLowerId(fact.m_Relative), fact.Critter.CommonName());
            }
            return Loc.Format(EatSentence, inFact.Parent.CommonName(), fact.Critter.CommonName());
        }

        static private int Compare(BFBase x, BFBase y)
        {
            int critterCompare = BestiaryDesc.SortById(((BFEat) x).Critter, ((BFEat) y).Critter);
            if (critterCompare != 0)
                return critterCompare;

            return CompareStressedPair(x, y);
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFEat fact = (BFEat) inFact;
            return fact.Critter.Icon();
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        protected override bool IsPair(BFBehavior inOther)
        {
            BFEat eat = inOther as BFEat;
            return eat != null && eat.Critter == Critter;
        }

        public override bool Optimize()
        {
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFEat>();
                if (pair != null)
                {
                    long compare = (long) Amount - (long) pair.Amount;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id, pair.Id, Amount);
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Less, QualCompare.More));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}