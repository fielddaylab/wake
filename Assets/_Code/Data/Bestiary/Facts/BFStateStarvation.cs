using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Starvation State Change")]
    public class BFStateStarvation : BFState
    {
        #region Inspector

        [Header("Starvation")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Food;
        [SerializeField] private uint m_Ticks = 0;
        [SerializeField] private QualitativeMapping m_QualMap = null;

        #endregion // Inspector

        [NonSerialized] private QualitativeValue m_QualTicks = QualitativeValue.None;

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public uint Ticks() { return m_Ticks; }

        public QualitativeMapping QualMap() { return m_QualMap; }
        public QualitativeValue QualTicks() { return m_QualTicks; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            throw new System.NotImplementedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new System.NotImplementedException();
        }

        protected override void GenerateQualitative()
        {
            base.GenerateQualitative();

            m_QualTicks = m_QualMap.Closest(m_Ticks);
        }
    }
}