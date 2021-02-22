using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Grow")]
    public class BFGrow : BFBehavior
    {
        #region Inspector

        [Header("Growth")]
        [SerializeField] private uint m_Amount = 0;
        [SerializeField] private QualitativeMapping m_QualMap = null;

        [Space]
        [SerializeField] private uint m_Interval = 0;

        #endregion // Inspector
        
        [NonSerialized] private QualitativeValue m_QualAmount = QualitativeValue.None;

        public uint Amount() { return m_Amount; }
        public uint Interval() { return m_Interval; }

        public QualitativeMapping QualMap() { return m_QualMap; }
        public QualitativeValue QualAmount() { return m_QualAmount; }

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

        public override bool HasSameSlot(BFBehavior inBehavior)
        {
            BFGrow grow = inBehavior as BFGrow;
            return grow != null;
        }

        protected override void GenerateQualitative()
        {
            base.GenerateQualitative();

            m_QualAmount = m_QualMap.Closest(m_Amount);
        }
    }
}