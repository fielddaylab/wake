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
        #region Inspector

        [Header("Produce")]
        [SerializeField] private WaterPropertyId m_Property = WaterPropertyId.Oxygen;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        public WaterPropertyId Target() { return m_Property; }
        public uint Amount() { return m_Amount; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            // TODO: localization!!

            bool bHasValue = inParams != null && inParams.Has(PlayerFactFlags.Stressed);

            bool kHasValue = inParams != null && inParams.Has(PlayerFactFlags.KnowValue);

            yield return BestiaryFactFragment.CreateNoun(Parent().CommonName());
            yield return BestiaryFactFragment.CreateVerb("Consumes");
            if(kHasValue)
            {
                yield return BestiaryFactFragment.CreateAmount(Amount());
            }
            yield return BestiaryFactFragment.CreateNoun(Services.Assets.WaterProp.Property(Target()).LabelId());
            if(bHasValue)
            {
                yield return BestiaryFactFragment.CreateWord(BestiaryFactFragmentType.Conjunction, "When");
                yield return BestiaryFactFragment.CreateNoun("Stressed");
            }
            
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            // TODO: localization!!!

            bool bHasValue = inParams != null && inParams.Has(PlayerFactFlags.Stressed);

            bool kHasValue = inParams != null && inParams.Has(PlayerFactFlags.KnowValue);

            using(var psb = PooledStringBuilder.Create())
            {
                // TODO: Variants

                psb.Builder.Append(Services.Loc.Localize(Parent().CommonName()))
                    .Append(" produces ");
                if(kHasValue)
                {
                    psb.Builder.Append(" " + Amount() + " ");
                }
                psb.Builder.Append(FormatValue(Target(), m_Amount)).Append(' ');
                psb.Builder.Append(" per tick");
                if(bHasValue)
                {
                    psb.Builder.Append("when stressed");
                }
                return psb.Builder.Flush();
            }
        }

        protected override int GetSortingOrder()
        {
            return 11;
        }
    }
}