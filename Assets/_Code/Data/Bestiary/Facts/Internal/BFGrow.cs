using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Grow")]
    public class BFGrow : BFBehavior
    {
        static private readonly TextId GrowVerb = "words.grow";
        static private readonly TextId GrowSentence = "factFormat.grow";
        static private readonly TextId GrowSentenceStressed = "factFormat.grow.stressed";

        #region Inspector

        [Header("Growth")]
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        [NonSerialized] private QualCompare m_Relative;

        public uint Amount() { return m_Amount; }

        public override void Hook(BestiaryDesc inParent)
        {
            base.Hook(inParent);

            if (OnlyWhenStressed())
            {
                var pair = FindPairedFact<BFGrow>();
                if (pair != null)
                {
                    float compare = (float) m_Amount - pair.m_Amount;
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
            return GrowVerb;
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultGrowIcon();
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
                return Loc.Format(GrowSentenceStressed, Parent().CommonName(), QualitativeLowerId(m_Relative));
            }
            return Loc.Format(GrowSentence, Parent().CommonName());
        }

        public override BFMode Mode()
        {
            return m_AutoGive ? BFMode.Always : (OnlyWhenStressed() ? BFMode.Player : BFMode.Always);
        }
    }
}