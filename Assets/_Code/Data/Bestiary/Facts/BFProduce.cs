using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Produce")]
    public class BFProduce : BFBehavior
    {
        static private readonly TextId ProduceVerb = "words.produce";
        static private readonly TextId ProduceSentence = "factFormat.produce";
        static private readonly TextId ProduceSentenceStressed = "factFormat.produce.stressed";

        #region Inspector

        [Header("Produce")]
        [SerializeField] private WaterPropertyId m_Property = WaterPropertyId.Oxygen;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        public WaterPropertyId Target() { return m_Property; }
        public uint Amount() { return m_Amount; }

        protected override TextId DefaultVerb()
        {
            return ProduceVerb;
        }

        protected override TextId DefaultSentence()
        {
            return OnlyWhenStressed() ? ProduceSentenceStressed : ProduceSentence;
        }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultProduceIcon();
        }

        protected override Sprite DefaultIcon()
        {
            return Property(m_Property).Icon();
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