using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProtoAqua.Experiment
{
    [CreateAssetMenu(menuName = "Aqualab/Experiment/Experiment Settings")]
    public class ExperimentSettings : TweakAsset
    {
        #region Types

        [Serializable]
        public class TankDefinition : IKeyValuePair<TankType, TankDefinition>
        {
            public TankType Tank;

            public ExpSubscreen[] Sequence;
            public SerializedHash32 LabelId;
            public SerializedHash32 ShortLabelId;
            public Sprite Icon;

            public string Condition;
            public BestiaryDescSize MaxSize;

            TankType IKeyValuePair<TankType, TankDefinition>.Key { get { return Tank; } }

            TankDefinition IKeyValuePair<TankType, TankDefinition>.Value { get { return this; } }

        }

        [Serializable]

        public class PropDefinition : IKeyValuePair<WaterPropertyId, PropDefinition>
        {
            public WaterPropertyId Id;

            public Color Color;

            public SerializedHash32 LabelId;

            public Sprite Icon;

            WaterPropertyId IKeyValuePair<WaterPropertyId, PropDefinition>.Key {get { return Id; } }
            PropDefinition IKeyValuePair<WaterPropertyId, PropDefinition>.Value {get { return this; } }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private TankDefinition[] m_TankDefs = null;

        [Header("Stressor Settings")]

        [SerializeField] private PropDefinition[] m_Props = null;

        [Header("Icon Colors")]
        [SerializeField] private Color m_EnabledButtonColor = Color.white;
        [SerializeField] private Color m_DisabledButtonColor = Color.white;
        
        [Header("Timing")]
        [SerializeField] private uint m_ThinkTickSpacing = 100;

        #endregion // Inspector

        public IEnumerable<TankDefinition> AllNonEmptyTanks()
        {
            for(int i = 0; i < m_TankDefs.Length; ++i)
            {
                if (m_TankDefs[i].Tank != TankType.None)
                    yield return m_TankDefs[i];
            }
        }

        public TankDefinition GetTank(TankType inType)
        {
            TankDefinition def;
            m_TankDefs.TryGetValue(inType, out def);
            return def;
        }

        public IEnumerable<PropDefinition> AllNonEmptyProperties()
        {
            for(int i = 0; i < m_Props.Length; ++i)
            {
                if (!m_Props[i].Id.Equals(WaterPropertyId.None))
                    yield return m_Props[i];
            }
        }

        public PropDefinition GetProperty(WaterPropertyId id) {
            PropDefinition def;
            m_Props.TryGetValue(id, out def);
            return def;
        }

        public Color SetupButtonColor(bool inbEnabled)
        {
            return inbEnabled ? m_EnabledButtonColor : m_DisabledButtonColor;
        }

        public uint ThinkSpacing() { return m_ThinkTickSpacing; }

        #region TweakAsset

        protected override void Apply()
        {
        }

        protected override void Remove()
        {
        }

        #endregion // TweakAsset
    }
}