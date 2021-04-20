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
        #region Inspector

        [Header("Eating")]
        [SerializeField] private BestiaryDesc m_TargetEntry = null;
        [SerializeField] private uint m_Amount = 0;

        #endregion // Inspector

        public BestiaryDesc Target() { return m_TargetEntry; }
        public uint Amount() { return m_Amount; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            // TODO: localization!!

            bool bHasValue = inParams != null && inParams.Has(PlayerFactFlags.KnowValue);

            yield return BestiaryFactFragment.CreateNoun(Parent().CommonName());
            yield return BestiaryFactFragment.CreateVerb("Eats");
            if (bHasValue)
                yield return BestiaryFactFragment.CreateAmount(FormatValue(WaterPropertyId.Food, m_Amount));
            yield return BestiaryFactFragment.CreateNoun(m_TargetEntry.CommonName());
            if (bHasValue)
                yield return BestiaryFactFragment.CreateAdjective("Per Tick");
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            // TODO: localization!!!

            bool bHasValue = inParams != null && inParams.Has(PlayerFactFlags.KnowValue);

            using(var psb = PooledStringBuilder.Create())
            {
                // TODO: Variants

                psb.Builder.Append(Services.Loc.Localize(Parent().CommonName()))
                    .Append(" eats ");
                if (bHasValue)
                    psb.Builder.Append(FormatValue(WaterPropertyId.Food, m_Amount)).Append(' ');
                psb.Builder.Append(Services.Loc.Localize(m_TargetEntry.CommonName()));
                if (bHasValue)
                    psb.Builder.Append(" per tick");

                return psb.Builder.Flush();
            }
        }

        public override bool HasSameSlot(BFBehavior inBehavior)
        {
            BFEat eat = inBehavior as BFEat;
            if (eat != null)
                return eat.m_TargetEntry == m_TargetEntry;

            return false;
        }

        protected override int GetSortingOrder()
        {
            return 10;
        }

        public override bool HasValue()
        {
            return true;
        }
    }
}