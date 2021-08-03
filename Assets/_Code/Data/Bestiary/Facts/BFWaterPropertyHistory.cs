using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Water Property History")]
    public class BFWaterPropertyHistory : BFBase
    {
        #region Inspector

        [Header("Property")]
        [SerializeField, AutoEnum] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        [SerializeField, AutoEnum] private BFGraphType m_Graph = BFGraphType.Flat;

        #endregion // Inspector

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public BFGraphType Graph() { return m_Graph; }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        internal override int GetSortingOrder()
        {
            return 0;
        }

        public override BFMode Mode()
        {
            return BFMode.Player;
        }

        protected override Sprite DefaultIcon()
        {
            return Property(m_PropertyId).Icon();
        }

        public override string GenerateSentence()
        {
            WaterPropertyDesc property = Property(m_PropertyId);
            return Loc.Format(property.EnvironmentHistoryFactFormat(), Parent().CommonName(), BestiaryUtils.GraphTypeToTextId(m_Graph), Parent().HistoricalRecordDuration());
        }
    }
}