using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public abstract class BFBehavior : BFBase
    {
        #region Inspector

        [Header("Behavior")]
        [SerializeField] private bool m_Stressed = false;

        #endregion // Inspector

        public bool OnlyWhenStressed() { return m_Stressed; }
        
        public bool CheckOverride(BFBehavior inBehavior, out BFBehavior outOverridden)
        {
            if (m_Stressed == inBehavior.m_Stressed || !HasSameSlot(inBehavior))
            {
                outOverridden = null;
                return false;
            }

            outOverridden = m_Stressed ? this : inBehavior;
            return true;
        }

        public virtual bool HasSameSlot(BFBehavior inBehavior)
        {
            return GetType() == inBehavior.GetType();
        }

        protected string FormatValue(WaterPropertyId inId, float inValue)
        {
            return Services.Assets.WaterProp.Property(inId)?.FormatValue(inValue);
        }
    }
}