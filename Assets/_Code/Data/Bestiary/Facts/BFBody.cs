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

        #endregion // Inspector

        public uint StartingMass() { return m_StartingMass; }
    }
}