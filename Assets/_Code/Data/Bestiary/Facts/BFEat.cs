using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Eats")]
    public class BFEat : BFBehavior
    {
        static private readonly TextId EatVerb = "words.eat";
        static private readonly TextId IsEatenByVerb = "words.isEatenBy";
        static private readonly TextId EatSentence = "factFormat.eat";
        static private readonly TextId EatSentenceStressed = "factFormat.eat.stressed";

        #region Inspector

        [Header("Eating")]
        [SerializeField, Required] private BestiaryDesc m_TargetEntry = null;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        [NonSerialized] private QualCompare m_Relative;

        public BestiaryDesc Target() { return m_TargetEntry; }
        public uint Amount() { return m_Amount; }

        public override void Hook(BestiaryDesc inParent)
        {
            base.Hook(inParent);

            if (OnlyWhenStressed())
            {
                var pair = FindPairedFact<BFEat>();
                if (pair != null)
                {
                    long compare = (long) m_Amount - (long) pair.m_Amount;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id(), pair.Id(), m_Amount);
                    m_Relative = MapDescriptor(compare, QualCompare.Less, QualCompare.More);
                }
            }
        }

        protected override TextId DefaultVerb()
        {
            return EatVerb;
        }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultConsumeIcon();
        }

        protected override Sprite DefaultIcon()
        {
            return m_TargetEntry.Icon();
        }

        protected override bool IsPair(BFBehavior inOther)
        {
            BFEat eat = inOther as BFEat;
            return eat != null && eat.m_TargetEntry == m_TargetEntry;
        }

        public override IEnumerable<BFFragment> GenerateFragments(BestiaryDesc inReference)
        {
            // TODO: localization!!
            if (inReference == null || inReference == Parent())
            {
                yield return BFFragment.CreateLocNoun(Parent().CommonName());
                yield return BFFragment.CreateLocVerb(Verb());
                if (OnlyWhenStressed())
                {
                    yield return BFFragment.CreateLocAdjective(QualitativeId(m_Relative));
                }
                yield return BFFragment.CreateLocNoun(m_TargetEntry.CommonName());
            }
            else
            {
                yield return BFFragment.CreateLocNoun(m_TargetEntry.CommonName());
                yield return BFFragment.CreateLocVerb(IsEatenByVerb);
                yield return BFFragment.CreateLocNoun(Parent().CommonName());
                if (OnlyWhenStressed())
                {
                    yield return BFFragment.CreateLocAdjective(QualitativeId(m_Relative));
                }
            }
        }

        public override string GenerateSentence()
        {
            TextId force = SentenceOverride();
            if (!force.IsEmpty)
                return Loc.Find(force);

            if (OnlyWhenStressed())
            {
                return Loc.Format(EatSentenceStressed, Parent().CommonName(), QualitativeLowerId(m_Relative), Target().CommonName());
            }
            return Loc.Format(EatSentence, Parent().CommonName(), Target().CommonName());
        }

        public override void CollectReferences(HashSet<StringHash32> outReferencedBestiary)
        {
            base.CollectReferences(outReferencedBestiary);
            outReferencedBestiary.Add(m_TargetEntry.Id());
        }

        internal override int GetSortingOrder()
        {
            return 10;
        }
    }
}