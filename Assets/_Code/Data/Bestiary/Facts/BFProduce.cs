using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
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
                var pair = FindPairedFact<BFProduce>();
                if (pair != null)
                {
                    long compare = (long) m_Amount - (long) pair.m_Amount;
                    Assert.True(compare != 0, "Facts '{0}' and '{1}' are paired but have the same value {1}", Id(), pair.Id(), m_Amount);
                    m_Relative = MapDescriptor(compare, QualCompare.Less, QualCompare.More);
                }
            }
        }

        protected override bool IsPair(BFBehavior inOther)
        {
            BFProduce produce = inOther as BFProduce;
            return produce != null && produce.m_Property == m_Property;
        }

        protected override TextId DefaultVerb()
        {
            return ProduceVerb;
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

            if (OnlyWhenStressed())
            {
                return Loc.Format(ProduceSentenceStressed, Parent().CommonName(), QualitativeLowerId(m_Relative), Property(m_Property).LabelId());
            }
            return Loc.Format(ProduceSentence, Parent().CommonName(), Property(m_Property).LabelId());
        }

        internal override int GetSortingOrder()
        {
            return 11;
        }
    }
}