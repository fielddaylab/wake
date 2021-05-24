using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Death Rate")]
    public class BFDeath : BFBehavior
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

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments()
        {
            throw new System.NotSupportedException();
        }

        public override string GenerateSentence()
        {
            throw new System.NotSupportedException();
        }
    }
}