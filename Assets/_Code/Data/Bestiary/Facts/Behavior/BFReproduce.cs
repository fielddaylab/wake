using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Reproduce")]
    public class BFReproduce : BFBehavior
    {
        #region Inspector

        [Header("Reproduction")]
        [Range(0, 5)] public float Amount = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFReproduce() : base(BFTypeId.Reproduce) { }

        #region Behavior

        static private readonly TextId ReproduceVerb = "words.reproduce";
        static private readonly TextId ReproduceSentence = "factFormat.reproduce";
        static private readonly TextId ReproduceSentenceStressed = "factFormat.reproduce.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Reproduce, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Reproduce, null, GenerateSentence, GenerateFragments);
            BFType.DefineEditor(BFTypeId.Reproduce, (Sprite) null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            BFReproduce fact = (BFReproduce) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(ReproduceVerb);
            if (fact.OnlyWhenStressed)
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(fact.m_Relative));
            }
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFReproduce fact = (BFReproduce) inFact;

            if (fact.OnlyWhenStressed)
            {
                return Loc.Format(ReproduceSentenceStressed, inFact.Parent.CommonName(), QualitativeLowerId(fact.m_Relative));
            }
            return Loc.Format(ReproduceSentence, inFact.Parent.CommonName());
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Optimize()
        {
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFReproduce>();
                if (pair != null)
                {
                    float compare = Amount - pair.Amount;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id, pair.Id, Amount);
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}