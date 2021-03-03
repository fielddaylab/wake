using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Body")]
    public class BFBody : BFInternal
    {
        #region Inspector

        [Header("Body")]
        [SerializeField] private uint m_StartingMass = 0;
        
        [Header("Display")]
        [SerializeField] private float m_MassDisplayScale = 1;
        [SerializeField] private uint m_PopulationSoftCap = 1000;
        [SerializeField] private uint m_PopulationSoftIncrement = 1;

        #endregion // Inspector

        public uint StartingMass() { return m_StartingMass; }

        public float MassDisplayScale() { return m_MassDisplayScale; }
        public uint PopulationSoftCap() { return m_PopulationSoftCap; }
        public uint PopulationSoftIncrement() { return m_PopulationSoftIncrement; }

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }
    }
}