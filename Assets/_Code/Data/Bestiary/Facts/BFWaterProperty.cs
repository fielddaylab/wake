using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Water")]
    public class BFWaterProperty : BFBase
    {
        #region Inspector

        [Header("Property")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        [SerializeField] private float m_Value = 0;
        [SerializeField] private float m_MaxValue = 0;

        #endregion // Inspector

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public float Value() { return m_Value; }
        public float MaxValue() { return m_MaxValue;}

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }


        public override BFMode Mode()
        {
            return BFMode.Always;
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            yield return BestiaryFactFragment.CreateNoun(m_PropertyId.ToString());
            yield return BestiaryFactFragment.CreateVerb("Is");
            yield return BestiaryFactFragment.CreateAmount(m_Value.ToString());
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new System.NotImplementedException();
        }
    }
}