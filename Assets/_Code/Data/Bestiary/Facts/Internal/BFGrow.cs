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

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultGrowIcon();
        }

        public override IEnumerable<BFFragment> GenerateFragments()
        {
            throw new NotImplementedException();
        }

        public override string GenerateSentence()
        {
            throw new NotImplementedException();
        }

        public override BFMode Mode()
        {
            return OnlyWhenStressed() ? BFMode.Player : BFMode.Internal;
        }
    }
}