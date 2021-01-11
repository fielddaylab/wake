using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Reproduce")]
    public class BFReproduce : BFBehavior
    {
        #region Inspector

        [Header("Reproduction")]
        [SerializeField] private uint m_Interval = 0;
        [SerializeField] private QualitativeMapping m_QualMap = null;

        [Space]
        [SerializeField] private uint m_Amount = 0;
        [SerializeField] private uint m_MinAge = 0;

        #endregion // Inspector
        
        [NonSerialized] private QualitativeValue m_QualInterval = QualitativeValue.None;

        public uint Interval() { return m_Interval; }
        
        public uint Amount() { return m_Amount; }
        public uint MinAge() { return m_MinAge; }

        public QualitativeMapping QualMap() { return m_QualMap; }
        public QualitativeValue QualInterval() { return m_QualInterval; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            throw new NotImplementedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new NotImplementedException();
        }

        public override bool IsIdentitical(PlayerFactParams inParams1, PlayerFactParams inParams2)
        {
            throw new System.NotImplementedException();
        }

        public override bool HasSameSlot(BFBehavior inBehavior)
        {
            BFGrow grow = inBehavior as BFGrow;
            return grow != null;
        }

        protected override void GenerateQualitative()
        {
            base.GenerateQualitative();

            m_QualInterval = m_QualMap.Closest(m_Interval);
        }
    }
}