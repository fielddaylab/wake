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
        [SerializeField] private uint m_MaximumMass = 0;

        #endregion // Inspector

        public uint StartingMass() { return m_StartingMass; }
        public uint MaximumMass() { return m_MaximumMass; }
    }
}