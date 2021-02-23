using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Starvation State Change")]
    public class BFStateStarvation : BFState
    {
        #region Inspector

        [Header("Starvation")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Food;
        [SerializeField] private uint m_Ticks = 0;

        #endregion // Inspector

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public uint Ticks() { return m_Ticks; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override BFMode Mode()
        {
            return BFMode.Internal;
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            throw new System.NotImplementedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new System.NotImplementedException();
        }
    }
}