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

        [Header("Scarcity Level")]
        [SerializeField] private uint m_ScarcityLevel = 0;

        #endregion // Inspector

        public uint Amount() { return m_Amount; }
        public uint ScarcityLevel() { return m_ScarcityLevel; }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments()
        {
            throw new NotImplementedException();
        }

        public override string GenerateSentence()
        {
            throw new NotImplementedException();
        }

        public override BFMode Mode()
        {
            return BFMode.Internal;
        }
    }
}