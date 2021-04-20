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

            yield return BestiaryFactFragment.CreateNoun(Parent().CommonName());
            yield return BestiaryFactFragment.CreateVerb("Consumes");
            yield return BestiaryFactFragment.CreateNoun(Services.Assets.WaterProp.Property(Target()).LabelId());
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            // TODO: localization!!!

            using(var psb = PooledStringBuilder.Create())
            {
                // TODO: Variants

                psb.Builder.Append(Services.Loc.Localize(Parent().CommonName()))
                    .Append(" produces ");
                psb.Builder.Append(FormatValue(Target(), m_Amount)).Append(' ');
                psb.Builder.Append(" per tick");

                return psb.Builder.Flush();
            }
        }
    }
}