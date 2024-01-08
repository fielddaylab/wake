using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Produce")]
    public class BFProduce : BFBehavior
    {
        #region Inspector

        [Header("Produce")]
        [AutoEnum] public WaterPropertyId Property = WaterPropertyId.Oxygen;
        public float Amount = 0;

        #endregion // Inspector

        private BFProduce() : base(BFTypeId.Produce) { }

        #region Behavior

        static public readonly TextId ProduceVerb = "words.produce";
        static private readonly TextId ProduceSentence = "factFormat.produce";
        static private readonly TextId ProduceSentenceStressed = "factFormat.produce.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Produce, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.HasRate, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Produce, null, GenerateDetails, GenerateFragments, null, (f) => ((BFProduce) f).Property);
            BFType.DefineEditor(BFTypeId.Produce, DefaultIcon, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFProduce fact = (BFProduce) inFact;


            if (Services.Loc.IsCurrentLanguageGendered()) {
                yield return BFFragment.CreateGenderedLocNoun(fact.Parent.CommonName(), fact.Parent.Gender());
            }
            else {
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            }
            yield return BFFragment.CreateLocVerb(ProduceVerb);
            yield return BFFragment.CreateAmount(BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property));

            if (Services.Loc.IsCurrentLanguageGendered())
            {
                //yield return BFFragment.CreateGenderedLocNoun(BestiaryUtils.Property(fact.Property).ShortLabelId(), BestiaryUtils.Property(fact.Property).GenderId());
                // Turns out we don't need gendered articles on water properties
                yield return BFFragment.CreateLocNoun(BestiaryUtils.Property(fact.Property).ShortLabelId());
            }
            else
            {
                yield return BFFragment.CreateLocNoun(BestiaryUtils.Property(fact.Property).ShortLabelId());
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFProduce fact = (BFProduce) inFact;
            WaterPropertyDesc property = BestiaryUtils.Property(fact.Property);

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = null;

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(ProduceSentenceStressed,
                    inFact.Parent.CommonName(),
                    BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property),
                    BestiaryUtils.Property(fact.Property).LabelId());
            }
            else
            {
                details.Description = Loc.Format(ProduceSentence,
                    inFact.Parent.CommonName(),
                    BestiaryUtils.FormatPropertyRate(fact.Amount, fact.Property),
                    BestiaryUtils.Property(fact.Property).LabelId());
            }

            return details;
        }

        static private int Compare(BFBase x, BFBase y)
        {
            int propCompare = WaterPropertyDB.SortByVisualOrder(((BFProduce) x).Property, ((BFProduce) y).Property);
            if (propCompare != 0)
                return propCompare;

            return CompareStressedPair(x, y);
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFProduce fact = (BFProduce) inFact;
            return BestiaryUtils.Property(fact.Property).Icon();
        }

        #endregion // Behavior
    }
}