using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Consume")]
    public class BFConsume : BFBehavior
    {
        #region Inspector

        [Header("Consume")]
        [AutoEnum] public WaterPropertyId Property = WaterPropertyId.Oxygen;
        public float Amount = 0;

        #endregion // Inspector

        private BFConsume() : base(BFTypeId.Consume) { }

        #region Behavior

        static public readonly TextId ConsumeVerb = "words.consume";
        static private readonly TextId ConsumeSentence = "factFormat.consume";
        static private readonly TextId ConsumeSentenceStressed = "factFormat.consume.stressed";

        static public readonly TextId ReduceVerb = "words.reduce";
        static private readonly TextId ReduceSentence = "factFormat.reduce";
        static private readonly TextId ReduceSentenceStressed = "factFormat.reduce.stressed";

        static public readonly TextId MascNoun = "words.masculineNoun";
        static public readonly TextId FemNoun = "words.feminineNoun";


        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Consume, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.HasRate, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Consume, null, GenerateDetails, GenerateFragments, null, (f) => ((BFConsume) f).Property);
            BFType.DefineEditor(BFTypeId.Consume, DefaultIcon, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFConsume fact = (BFConsume) inFact;
            bool bIsLight = fact.Property == WaterPropertyId.Light;

            if (Services.Loc.IsCurrentLanguageGendered()) { 
                yield return BFFragment.CreateGenderedLocNoun(fact.Parent.CommonName(), fact.Parent.Gender());
            }
            else {
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            }
            yield return BFFragment.CreateLocVerb(bIsLight ? ReduceVerb : ConsumeVerb);
            if (!bIsLight)
            {
                yield return BFFragment.CreateAmount(BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property));
            }
            if (Services.Loc.IsCurrentLanguageGendered()) {
                // yield return BFFragment.CreateGenderedLocNoun(BestiaryUtils.Property(fact.Property).ShortLabelId(), BestiaryUtils.Property(fact.Property).GenderId());
                // Turns out we don't need gendered articles on water properties
                yield return BFFragment.CreateLocNoun(BestiaryUtils.Property(fact.Property).ShortLabelId());
            }
            else {
                yield return BFFragment.CreateLocNoun(BestiaryUtils.Property(fact.Property).ShortLabelId());
            }
            if (bIsLight)
            {
                yield return BFFragment.CreateAmount(BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFConsume fact = (BFConsume) inFact;
            bool bIsLight = fact.Property == WaterPropertyId.Light;
            WaterPropertyDesc property = BestiaryUtils.Property(fact.Property);

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = null;

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(bIsLight ? ReduceSentenceStressed : ConsumeSentenceStressed,
                    inFact.Parent.CommonName(),
                    BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property),
                    property.LabelId());
            }
            else
            {
                details.Description = Loc.Format(bIsLight ? ReduceSentence : ConsumeSentence,
                    inFact.Parent.CommonName(),
                    BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property),
                    property.LabelId());
            }

            return details;
        }

        static private int Compare(BFBase x, BFBase y)
        {
            int propCompare = WaterPropertyDB.SortByVisualOrder(((BFConsume) x).Property, ((BFConsume) y).Property);
            if (propCompare != 0)
                return propCompare;

            return CompareStressedPair(x, y);
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFConsume fact = (BFConsume) inFact;
            return BestiaryUtils.Property(fact.Property).Icon();
        }

        #endregion // Behavior
    }
}