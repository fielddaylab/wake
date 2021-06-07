using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Reproduce")]
    public class BFReproduce : BFBehavior
    {
        static private readonly TextId ReproduceVerb = "words.reproduce";
        static private readonly TextId ReproduceSentence = "factFormat.reproduce";
        static private readonly TextId ReproduceSentenceStressed = "factFormat.reproduce.stressed";

        #region Inspector

        [Header("Reproduction")]
        [SerializeField, Range(0, 5)] private float m_Amount = 0;

        #endregion // Inspector

        [NonSerialized] private QualCompare m_Relative;

        public float Amount() { return m_Amount; }

        public override void Hook(BestiaryDesc inParent)
        {
            base.Hook(inParent);

            if (OnlyWhenStressed())
            {
                var pair = FindPairedFact<BFReproduce>();
                if (pair != null)
                {
                    float compare = m_Amount - pair.m_Amount;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id(), pair.Id(), m_Amount);
                    m_Relative = MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster);
                }
            }
        }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        protected override TextId DefaultVerb()
        {
            return ReproduceVerb;
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultReproduceIcon();
        }

        public override IEnumerable<BFFragment> GenerateFragments()
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
                return Loc.Format(ReproduceSentenceStressed, Parent().CommonName(), QualitativeLowerId(m_Relative));
            }
            return Loc.Format(ReproduceSentence, Parent().CommonName());
        }

        public override BFMode Mode()
        {
            return m_AutoGive ? BFMode.Always : (OnlyWhenStressed() ? BFMode.Player : BFMode.Always);
        }
    }
}