using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Eats")]
    public class BFEat : BFBehavior
    {
        static private readonly TextId EatVerb = "words.eat";
        static private readonly TextId EatSentence = "factFormat.eat";
        static private readonly TextId EatSentenceStressed = "factFormat.eat.stressed";

        #region Inspector

        [Header("Eating")]
        [SerializeField, Required] private BestiaryDesc m_TargetEntry = null;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        public BestiaryDesc Target() { return m_TargetEntry; }
        public uint Amount() { return m_Amount; }

        protected override TextId DefaultVerb()
        {
            return EatVerb;
        }

        protected override TextId DefaultSentence()
        {
            return OnlyWhenStressed() ? EatSentenceStressed : EatSentence;
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

        public override IEnumerable<BFFragment> GenerateFragments()
        {
            // TODO: localization!!

            yield return BFFragment.CreateLocNoun(Parent().CommonName());
            yield return BFFragment.CreateLocVerb(Verb());
            yield return BFFragment.CreateAmount(Property(WaterPropertyId.Food).FormatValue(m_Amount));
            yield return BFFragment.CreateLocNoun(m_TargetEntry.CommonName());
        }

        public override string GenerateSentence()
        {
            return Loc.Format(SentenceFormat(), Parent().CommonName(), Property(WaterPropertyId.Food).FormatValue(m_Amount), m_TargetEntry.CommonName());
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