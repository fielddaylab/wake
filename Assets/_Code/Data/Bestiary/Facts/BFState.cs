using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Range State Change")]
    public class BFState : BFBase
    {
        #region Inspector

        [Header("Property Range")]
        [SerializeField] private WaterPropertyId m_PropertyId = WaterPropertyId.Temperature;
        
        [Header("Alive State")]
        [SerializeField] private float m_MinSafe = 0;
        [SerializeField] private float m_MaxSafe = 0;

        [Header("Stressed State")]
        [SerializeField] private bool m_HasDeath = false;
        [SerializeField, ShowIfField("m_HasDeath")] private float m_MinStressed = float.MinValue;
        [SerializeField, ShowIfField("m_HasDeath")] private float m_MaxStressed = float.MaxValue;

        #endregion // Inspector

        [NonSerialized] private ActorStateTransitionRange m_Range;

        public WaterPropertyId PropertyId() { return m_PropertyId; }
        public ActorStateTransitionRange Range() { return m_Range; }
        public bool HasDeath() { return m_HasDeath; }

        public override void Hook(BestiaryDesc inParent)
        {
            base.Hook(inParent);

            m_Range.Reset();

            m_Range.AliveMin = m_MinSafe;
            m_Range.AliveMax = m_MaxSafe;

            if (m_HasDeath)
            {
                m_Range.StressedMin = m_MinStressed;
                m_Range.StressedMax = m_MaxStressed;
            }
        }

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

            return Loc.Format(property.StateChangeStressOnlyFormat(), Parent().CommonName(), property.FormatValue(m_MinSafe), property.FormatValue(m_MaxSafe));
        }
    }
}