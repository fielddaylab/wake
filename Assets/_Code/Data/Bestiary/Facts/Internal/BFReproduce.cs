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

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public override Sprite GraphIcon()
        {
            return Services.Assets.Bestiary.DefaultReproduceIcon();
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