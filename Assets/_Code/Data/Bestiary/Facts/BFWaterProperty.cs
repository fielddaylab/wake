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

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        internal override int GetSortingOrder()
        {
            return (int) m_PropertyId;
        }

        public override BFMode Mode()
        {
            return BFMode.Always;
        }

        public override string GenerateSentence()
        {
            WaterPropertyDesc property = Property(m_PropertyId);
            return Loc.Format(property.EnvironmentFactFormat(), Parent().CommonName(), property.FormatValue(m_Value));
        }
    }
}