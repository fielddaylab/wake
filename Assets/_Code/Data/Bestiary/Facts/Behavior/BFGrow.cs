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
            BFType.DefineAttributes(BFTypeId.Grow, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Grow, null, GenerateSentence, GenerateFragments);
            BFType.DefineEditor(BFTypeId.Grow, (Sprite) null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            BFGrow fact = (BFGrow) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(GrowVerb);
            if (fact.OnlyWhenStressed)
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
            }
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFGrow fact = (BFGrow) inFact;

            if (fact.OnlyWhenStressed)
            {
                return Loc.Format(GrowSentenceStressed, inFact.Parent.CommonName(), QualitativeLowerId(fact.m_Relative));
            }
            return Loc.Format(GrowSentence, inFact.Parent.CommonName());
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
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id, pair.Id, Amount);
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}