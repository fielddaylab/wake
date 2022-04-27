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
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFConsume() : base(BFTypeId.Consume) { }

        #region Behavior

        static public readonly TextId ConsumeVerb = "words.consume";
        static private readonly TextId ConsumeSentence = "factFormat.consume";
        static private readonly TextId ConsumeSentenceStressed = "factFormat.consume.stressed";

        static public readonly TextId ReduceVerb = "words.reduce";
        static private readonly TextId ReduceSentence = "factFormat.reduce";
        static private readonly TextId ReduceSentenceStressed = "factFormat.reduce.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Consume, BFShapeId.Behavior, BFFlags.IsBehavior, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Consume, null, GenerateDetails, GenerateFragments, null, (f) => ((BFConsume) f).Property);
            BFType.DefineEditor(BFTypeId.Consume, DefaultIcon, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFConsume fact = (BFConsume) inFact;
            bool bIsLight = fact.Property == WaterPropertyId.Light;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(bIsLight ? ReduceVerb : ConsumeVerb);
            yield return BFFragment.CreateLocNoun(BestiaryUtils.Property(fact.Property).LabelId());
            if (BFType.HasPair(inFlags))
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFConsume fact = (BFConsume) inFact;
            bool bIsLight = fact.Property == WaterPropertyId.Light;
            WaterPropertyDesc property = BestiaryUtils.Property(fact.Property);

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = property.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(bIsLight ? ReduceSentenceStressed : ConsumeSentenceStressed, inFact.Parent.CommonName(), QualitativeId(fact.m_Relative), property.LabelId());
            }
            else
            {
                details.Description = Loc.Format(bIsLight ? ReduceSentence : ConsumeSentence, inFact.Parent.CommonName(), property.LabelId());
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

        #if UNITY_EDITOR

        protected override bool IsPair(BFBehavior inOther)
        {
            BFConsume consume = inOther as BFConsume;
            return consume != null && consume.Property == Property;
        }

        public override bool Bake(BakeFlags flags)
        {
            bool bChanged = false;
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFConsume>();
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