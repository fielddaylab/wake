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

        #endregion // Inspector

        public uint Amount() { return m_Amount; }

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

        public override BFMode Mode()
        {
            return BFMode.Internal;
        }
    }
}