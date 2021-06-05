using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Consume")]
    public class BFConsume : BFBehavior
    {
        static private readonly TextId ConsumeVerb = "words.consume";
        static private readonly TextId ConsumeSentence = "factFormat.consume";
        static private readonly TextId ConsumeSentenceStressed = "factFormat.consume.stressed";

        static private readonly TextId ReduceVerb = "words.reduce";
        static private readonly TextId ReduceSentence = "factFormat.reduce";
        static private readonly TextId ReduceSentenceStressed = "factFormat.reduce.stressed";

        #region Inspector

        [Header("Produce")]
        [SerializeField, AutoEnum] private WaterPropertyId m_Property = WaterPropertyId.Oxygen;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        [NonSerialized] private QualCompare m_Relative;

        public WaterPropertyId Target() { return m_Property; }
        public uint Amount() { return m_Amount; }

        public override void Hook(BestiaryDesc inParent)
        {
            base.Hook(inParent);

            if (OnlyWhenStressed())
            {
                var pair = FindPairedFact<BFConsume>();
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
            return m_Property == WaterPropertyId.Light ? ReduceVerb : ConsumeVerb;
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
            if (OnlyWhenStressed())
            {
                yield return BFFragment.CreateLocAdjective(QualitativeId(m_Relative));
            }
            yield return BFFragment.CreateLocNoun(Property(m_Property).LabelId());
        }

        public override string GenerateSentence()
        {
            TextId force = SentenceOverride();
            if (!force.IsEmpty)
                return Loc.Find(force);

            bool bIsLight = m_Property == WaterPropertyId.Light;

            if (OnlyWhenStressed())
            {
                return Loc.Format(bIsLight ? ReduceSentenceStressed : ConsumeSentenceStressed, Parent().CommonName(), QualitativeLowerId(m_Relative), Property(m_Property).LabelId());
            }
            return Loc.Format(bIsLight ? ReduceSentence : ConsumeSentence, Parent().CommonName(), Property(m_Property).LabelId());
        }

        internal override int GetSortingOrder()
        {
            return 11;
        }
    }
}