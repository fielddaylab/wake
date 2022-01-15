using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Reproduce")]
    public class BFReproduce : BFBehavior
    {
        #region Inspector

        [Header("Reproduction")]
        [Range(0, 5)] public float Amount = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFReproduce() : base(BFTypeId.Reproduce) { }

        #region Behavior

        static public readonly TextId ReproduceVerb = "words.reproduce";
        static private readonly TextId ReproduceSentence = "factFormat.reproduce";
        static private readonly TextId ReproduceSentenceStressed = "factFormat.reproduce.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Reproduce, BFShapeId.Behavior, BFFlags.IsBehavior, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Reproduce, null, GenerateDetails, GenerateFragments, null, null);
            BFType.DefineEditor(BFTypeId.Reproduce, null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            BFReproduce fact = (BFReproduce) inFact;

            yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            yield return BFFragment.CreateLocVerb(ReproduceVerb);
            if (fact.OnlyWhenStressed)
            {
                yield return BFFragment.CreateLocAdjective(QualitativeLowerId(fact.m_Relative));
            }
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFReproduce fact = (BFReproduce) inFact;
            
            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Parent.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(ReproduceSentenceStressed, inFact.Parent.CommonName(), QualitativeLowerId(fact.m_Relative));
            }
            else
            {
                details.Description = Loc.Format(ReproduceSentence, inFact.Parent.CommonName());
            }

            return details;
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