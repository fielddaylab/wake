using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Body")]
    public class BFBody : BFInternal
    {
        #region Inspector

        [Header("Body")]
        [SerializeField, FormerlySerializedAs("m_StartingMass")] private uint m_MassPerPopulation = 0;
        
        [Header("Display")]
        [SerializeField] private float m_MassDisplayScale = 1;
        [SerializeField] private uint m_PopulationSoftCap = 1000;
        [SerializeField] private uint m_PopulationSoftIncrement = 1;
        [SerializeField] private uint m_PopulationHardCap = 1000;

        #endregion // Inspector

        public uint MassPerPopulation() { return m_MassPerPopulation; }

        public float MassDisplayScale() { return m_MassDisplayScale; }
        public uint PopulationSoftCap() { return m_PopulationSoftCap; }
        public uint PopulationHardCap() { return m_PopulationHardCap; }

        public uint PopulationSoftIncrement() { return m_PopulationSoftIncrement; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }
    }
}