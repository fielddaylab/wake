using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Age State Change")]
    public class BFStateAge : BFState
    {
        #region Inspector

        [Header("Age")]
        [SerializeField, Range(0, 1)] private float m_Proportion = 0;

        #endregion // Inspector

        public float Proportion() { return m_Proportion; }

        public override BFMode Mode()
        {
            return BFMode.Internal;
        }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            throw new System.NotSupportedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new System.NotSupportedException();
        }
    }
}