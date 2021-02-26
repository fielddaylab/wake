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
        [SerializeField] private float m_Amount = 0;

        #endregion // Inspector
        
        public float Amount() { return m_Amount; }

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