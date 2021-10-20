using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Death Rate")]
    public class BFDeath : BFBehavior
    {
        #region Inspector

        [Header("Death Rate")]
        [Range(0, 1)] public float Proportion = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFDeath() : base(BFTypeId.Death) { }

        #region Behavior

        static public readonly TextId DeathVerb = "words.death";
        static private readonly TextId DeathSentence = "factFormat.death";
        static private readonly TextId DeathSentenceStressed = "factFormat.death.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Death, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Death, null, GenerateSentence, GenerateFragments);
            BFType.DefineEditor(BFTypeId.Death, (Sprite) null, BFMode.Internal);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            BFDeath fact = (BFDeath) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(DeathVerb);
            if (fact.OnlyWhenStressed)
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
            }
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFDeath fact = (BFDeath) inFact;

            if (fact.OnlyWhenStressed)
            {
                return Loc.Format(DeathSentenceStressed, inFact.Parent.CommonName(), QualitativeLowerId(fact.m_Relative));
            }
            return Loc.Format(DeathSentence, inFact.Parent.CommonName());
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Optimize()
        {
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFDeath>();
                if (pair != null)
                {
                    float compare = Proportion - pair.Proportion;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id, pair.Id, Proportion);
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}