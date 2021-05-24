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

        public override IEnumerable<BestiaryFactFragment> GenerateFragments()
        {
            // TODO: localization!!

            yield return BestiaryFactFragment.CreateLocNoun(Parent().CommonName());
            yield return BestiaryFactFragment.CreateLocVerb(Verb());
            yield return BestiaryFactFragment.CreateAmount(Property(m_Property).FormatValue(m_Amount));
            yield return BestiaryFactFragment.CreateLocNoun(Property(m_Property).LabelId());
            yield return BestiaryFactFragment.CreateLocAdjective("words.perTick");
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