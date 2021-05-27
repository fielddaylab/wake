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
            public TextId LabelId;
            public TextId ShortLabelId;
            public Sprite Icon;
            public bool SingleCritter;
            public bool TankOn;

            public string Condition;
            public BestiaryDescSize MaxSize;

            TankType IKeyValuePair<TankType, TankDefinition>.Key { get { return Tank; } }
            TankDefinition IKeyValuePair<TankType, TankDefinition>.Value { get { return this; } }

        }

        #endregion // Types

        #region Inspector

        [SerializeField] private TankDefinition[] m_TankDefs = null;

        [Header("Icon Colors")]
        [SerializeField] private Color m_EnabledButtonColor = Color.white;
        [SerializeField] private Color m_DisabledButtonColor = Color.white;
        
        [Header("Timing")]
        [SerializeField] private uint m_ThinkTickSpacing = 100;

        #endregion // Inspector

        public ListSlice<TankDefinition> TankTypes()
        {
            return m_TankDefs;
        }

        public TankDefinition GetTank(TankType inType)
        {
            TankDefinition def;
            m_TankDefs.TryGetValue(inType, out def);
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