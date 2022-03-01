using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
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
            BFType.DefineAttributes(BFTypeId.Grow, BFShapeId.Behavior, BFFlags.IsBehavior, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Grow, null, GenerateDetails, GenerateFragments, null, null);
            BFType.DefineEditor(BFTypeId.Grow, null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            BFGrow fact = (BFGrow) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(GrowVerb);
            if (fact.OnlyWhenStressed)
            {
                yield return BFFragment.CreateLocAdjective(QualitativeLowerId(fact.m_Relative));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFGrow fact = (BFGrow) inFact;

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Parent.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(GrowSentenceStressed, inFact.Parent.CommonName(), QualitativeLowerId(fact.m_Relative));
            }
            else
            {
                details.Description = Loc.Format(GrowSentence, inFact.Parent.CommonName());
            }

            return details;
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Optimize()
        {
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFGrow>();
                if (pair != null)
                {
                    float compare = (float) Amount - pair.Amount;
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster, QualCompare.SameRate));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}