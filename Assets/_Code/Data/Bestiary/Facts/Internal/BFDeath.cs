using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Death Rate")]
    public class BFDeath : BFBehavior
    {
        static private readonly TextId DeathVerb = "words.death";
        static private readonly TextId DeathSentence = "factFormat.death";
        static private readonly TextId DeathSentenceStressed = "factFormat.death.stressed";

        #region Inspector

        [Header("Death Rate")]
        [SerializeField, Range(0, 1)] private float m_Proportion = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        public float Proportion() { return m_Proportion; }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        protected override TextId DefaultVerb()
        {
            return DeathVerb;
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultDeathIcon();
        }

        public override IEnumerable<BFFragment> GenerateFragments(BestiaryDesc _)
        {
            yield return BFFragment.CreateLocNoun(Parent().CommonName());
            yield return BFFragment.CreateLocVerb(Verb());
            if (OnlyWhenStressed())
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(m_Relative));
            }
        }

        public override string GenerateSentence()
        {
            TextId force = SentenceOverride();
            if (!force.IsEmpty)
                return Loc.Find(force);

            if (OnlyWhenStressed())
            {
                return Loc.Format(DeathSentenceStressed, Parent().CommonName(), QualitativeLowerId(m_Relative));
            }
            return Loc.Format(DeathSentence, Parent().CommonName());
        }

        public override BFMode Mode()
        {
            return m_AutoGive ? BFMode.Always : (OnlyWhenStressed() ? BFMode.Player : BFMode.Internal);
        }

        public override BFDiscoveredFlags DefaultInformationFlags()
        {
            return BFDiscoveredFlags.All;
        }

        #if UNITY_EDITOR

        public override bool Optimize()
        {
            if (OnlyWhenStressed())
            {
                var pair = FindPairedFact<BFDeath>();
                if (pair != null)
                {
                    float compare = m_Proportion - pair.m_Proportion;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id(), pair.Id(), m_Proportion);
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}