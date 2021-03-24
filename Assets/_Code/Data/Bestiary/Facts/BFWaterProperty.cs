using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Water")]
    public class BFWaterProperty : BFBase
    {
        #region Inspector

        [Header("Property")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        [SerializeField] private float m_Value = 0;

        #endregion // Inspector

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public float Value() { return m_Value; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        protected override int GetSortingOrder()
        {
            return (int) m_PropertyId;
        }

        public override BFMode Mode()
        {
            return BFMode.Always;
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            yield return BestiaryFactFragment.CreateNoun(Services.Assets.WaterProp.Property(m_PropertyId).LabelId());
            yield return BestiaryFactFragment.CreateVerb("Is");
            yield return BestiaryFactFragment.CreateAmount(m_Value.ToString());
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            var prop = Services.Assets.WaterProp.Property(m_PropertyId);

            using(var psb = PooledStringBuilder.Create())
            {
                psb.Builder.Append(Services.Loc.Localize(prop.LabelId()))
                    .Append(" in ")
                    .Append(Services.Loc.Localize(Parent().CommonName()))
                    .Append(" is ")
                    .Append(m_Value.ToString());

                return psb.ToString();
            }
        }
    }
}