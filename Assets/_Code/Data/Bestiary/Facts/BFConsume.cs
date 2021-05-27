using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Consume")]
    public class BFConsume : BFBehavior
    {
        static private readonly TextId ConsumeVerb = "words.consume";
        static private readonly TextId ConsumeSentence = "factFormat.consume";
        static private readonly TextId ConsumeSentenceStressed = "factFormat.consume.stressed";

        #region Inspector

        [Header("Produce")]
        [SerializeField] private WaterPropertyId m_Property = WaterPropertyId.Oxygen;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        public WaterPropertyId Target() { return m_Property; }
        public uint Amount() { return m_Amount; }

        protected override TextId DefaultVerb()
        {
            return ConsumeVerb;
        }

        protected override TextId DefaultSentence()
        {
            return OnlyWhenStressed() ? ConsumeSentenceStressed : ConsumeSentence;
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
            return Property(m_Property).Icon();
        }

        protected override bool IsPair(BFBehavior inOther)
        {
            BFConsume consume = inOther as BFConsume;
            return consume != null && consume.m_Property == m_Property;
        }

        public override IEnumerable<BFFragment> GenerateFragments()
        {
            // TODO: localization!!

            yield return BFFragment.CreateLocNoun(Parent().CommonName());
            yield return BFFragment.CreateLocVerb(Verb());
            yield return BFFragment.CreateAmount(Property(m_Property).FormatValue(m_Amount));
            yield return BFFragment.CreateLocNoun(Property(m_Property).LabelId());
        }

        public override string GenerateSentence()
        {
            return Loc.Format(SentenceFormat(), Parent().CommonName(), Property(m_Property).FormatValue(m_Amount), Property(m_Property).LabelId());
        }

        internal override int GetSortingOrder()
        {
            return 11;
        }
    }
}