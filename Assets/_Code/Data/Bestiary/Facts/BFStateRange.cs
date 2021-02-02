using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Range State Change")]
    public class BFStateRange : BFState
    {
        #region Inspector

        [Header("Property Range")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        [SerializeField] private float m_MinSafe = 0;
        [SerializeField] private float m_MaxSafe = 0;
        [SerializeField] private QualitativeMapping m_QualMap = null;

        #endregion // Inspector

        [NonSerialized] private QualitativeValue m_QualMinSafe = QualitativeValue.None;
        [NonSerialized] private QualitativeValue m_QualMaxSafe = QualitativeValue.None;

        public WaterPropertyId PropertyId() { return m_PropertyId; }

        public float MinSafe() { return m_MinSafe; }
        public float MaxSafe() { return m_MaxSafe; }

        public QualitativeMapping QualMap() { return m_QualMap; }
        public QualitativeValue QualMinSafe() { return m_QualMinSafe; }
        public QualitativeValue QualMaxSafe() { return m_QualMaxSafe; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            yield return BestiaryFactFragment.CreateAmount(10f);
            //throw new System.NotImplementedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new System.NotImplementedException();
        }

        protected override void GenerateQualitative()
        {
            base.GenerateQualitative();

            m_QualMinSafe = m_QualMap.Closest(m_MinSafe);
            m_QualMaxSafe = m_QualMap.Closest(m_MaxSafe);
        }
    }
}