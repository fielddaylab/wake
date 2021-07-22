using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Range State Change")]
    public class BFState : BFBase, IOptimizableAsset
    {
        #region Inspector

        [Header("Property Range")]
        [SerializeField, AutoEnum] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        
        [Header("Alive State")]
        [SerializeField] private bool m_HasStressed = true;
        [SerializeField, ShowIfField("m_HasStressed")] private float m_MinSafe = 0;
        [SerializeField, ShowIfField("m_HasStressed")] private float m_MaxSafe = 0;

        [Header("Stressed State")]
        [SerializeField] private bool m_HasDeath = false;
        [SerializeField, ShowIfField("m_HasDeath")] private float m_MinStressed = float.MinValue;
        [SerializeField, ShowIfField("m_HasDeath")] private float m_MaxStressed = float.MaxValue;

        #endregion // Inspector

        [SerializeField, HideInInspector] private ActorStateTransitionRange m_Range;

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public ActorStateTransitionRange Range() { return m_Range; }
        public bool HasStressed() { return m_HasStressed; }
        public bool HasDeath() { return m_HasDeath; }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        protected override Sprite DefaultIcon()
        {
            return Property(m_PropertyId).Icon();
        }

        public override string GenerateSentence()
        {
            WaterPropertyDesc property = Property(m_PropertyId);
            if (m_HasDeath)
            {
                return Loc.Format(property.StateChangeFormat(), Parent().CommonName()
                    , property.FormatValue(m_MinSafe), property.FormatValue(m_MaxSafe)
                    , property.FormatValue(m_MinStressed), property.FormatValue(m_MaxStressed)
                );
            }

            if (m_HasStressed)
            {
                return Loc.Format(property.StateChangeStressOnlyFormat(), Parent().CommonName(), property.FormatValue(m_MinSafe), property.FormatValue(m_MaxSafe));
            }

            return Loc.Format(property.StateChangeUnaffectedFormat(), Parent().CommonName());
        }

        internal override int GetSortingOrder()
        {
            return (int) m_PropertyId;
        }

        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return -9; } }

        bool IOptimizableAsset.Optimize()
        {
            ActorStateTransitionRange range = ActorStateTransitionRange.Default;

            if (m_HasStressed)
            {
                range.AliveMin = m_MinSafe;
                range.AliveMax = m_MaxSafe;
            }

            if (m_HasDeath)
            {
                range.StressedMin = m_MinStressed;
                range.StressedMax = m_MaxStressed;
            }

            return Ref.Replace(ref m_Range, range);
        }

        #endif // UNITY_EDITOR
    }
}