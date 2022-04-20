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
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFProduce() : base(BFTypeId.Produce) { }

        #region Behavior

        static public readonly TextId ProduceVerb = "words.produce";
        static private readonly TextId ProduceSentence = "factFormat.produce";
        static private readonly TextId ProduceSentenceStressed = "factFormat.produce.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Produce, BFShapeId.Behavior, BFFlags.IsBehavior, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Produce, null, GenerateDetails, GenerateFragments, null, (f) => ((BFProduce) f).Property);
            BFType.DefineEditor(BFTypeId.Produce, DefaultIcon, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFProduce fact = (BFProduce) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(ProduceVerb);
            yield return BFFragment.CreateLocNoun(BestiaryUtils.Property(fact.Property).LabelId());
            if (BFType.HasPair(inFlags))
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFProduce fact = (BFProduce) inFact;
            WaterPropertyDesc property = BestiaryUtils.Property(fact.Property);

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = property.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(ProduceSentenceStressed, inFact.Parent.CommonName(), QualitativeId(fact.m_Relative), BestiaryUtils.Property(fact.Property).LabelId());
            }
            else
            {
                details.Description = Loc.Format(ProduceSentence, inFact.Parent.CommonName(), BestiaryUtils.Property(fact.Property).LabelId());
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

        #if UNITY_EDITOR

        protected override bool IsPair(BFBehavior inOther)
        {
            BFProduce produce = inOther as BFProduce;
            return produce != null && produce.Property == Property;
        }

        public override bool Bake(BakeFlags flags)
        {
            bool bChanged = false;
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFProduce>();
                if (pair != null)
                {
                    float compare = Amount - pair.Amount;
                    bChanged |= Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster, QualCompare.SameRate));
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