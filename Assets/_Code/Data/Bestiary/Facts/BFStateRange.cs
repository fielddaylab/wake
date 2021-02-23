using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Range State Change")]
    public class BFStateRange : BFState
    {
        #region Inspector

        [Header("Property Range")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        [SerializeField] private float m_MinSafe = 0;
        [SerializeField] private float m_MaxSafe = 0;

        #endregion // Inspector

        public WaterPropertyId PropertyId() { return m_PropertyId; }

        public float MinSafe() { return m_MinSafe; }
        public float MaxSafe() { return m_MaxSafe; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            yield return BestiaryFactFragment.CreateAmount(10f);
            //throw new System.NotImplementedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            var result = "is least " + TargetState().ToString() + " at " + MinSafe().ToString() + " and most " + TargetState().ToString() + " at " + MaxSafe().ToString();

            return result;

            // throw new System.NotImplementedException();
        }
    }
}