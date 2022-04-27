using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Grow")]
    public class BFGrow : BFBehavior
    {
        #region Inspector

        [Header("Growth")]
        public uint Amount = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFGrow() : base(BFTypeId.Grow) { }

        #region Behavior

        static public readonly TextId GrowVerb = "words.grow";
        static private readonly TextId GrowSentence = "factFormat.grow";
        static private readonly TextId GrowSentenceStressed = "factFormat.grow.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Grow, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.SelfTarget, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Grow, null, GenerateDetails, GenerateFragments, null, null);
            BFType.DefineEditor(BFTypeId.Grow, null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFGrow fact = (BFGrow) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(GrowVerb);
            if (fact.OnlyWhenStressed)
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFGrow fact = (BFGrow) inFact;

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Parent.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(GrowSentenceStressed, inFact.Parent.CommonName(), QualitativeId(fact.m_Relative));
            }
            else
            {
                details.Description = Loc.Format(GrowSentence, inFact.Parent.CommonName());
            }

            return details;
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Bake(BakeFlags flags)
        {
            bool bChanged = false;
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFGrow>();
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