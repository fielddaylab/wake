using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProtoAqua.Experiment
{
    [CreateAssetMenu(menuName = "Prototype/Experiment/Experiment Settings")]
    public class ExperimentSettings : TweakAsset
    {
        #region Types

        [Serializable]
        public class TankDefinition : IKeyValuePair<TankType, TankDefinition>
        {
            public TankType Tank;
            public string LabelId;
            public string ShortLabelId;
            public Sprite Icon;
            public string Condition;

            TankType IKeyValuePair<TankType, TankDefinition>.Key { get { return Tank; } }

            TankDefinition IKeyValuePair<TankType, TankDefinition>.Value { get { return this; } }
        }

        [Serializable]
        public class EcoDefinition : IKeyValuePair<StringHash32, EcoDefinition>
        {
            public string Id;
            public string LabelId;
            public string ShortLabelId;
            public Sprite Icon;
            public string Condition;

            StringHash32 IKeyValuePair<StringHash32, EcoDefinition>.Key { get { return Id; } }

            EcoDefinition IKeyValuePair<StringHash32, EcoDefinition>.Value { get { return this; } }
        }

        [Serializable]
        public class ActorDefinition : IKeyValuePair<StringHash32, ActorDefinition>
        {
            public string Id;
            public string LabelId;
            public string ShortLabelId;
            public Sprite Icon;
            public string Condition;

            StringHash32 IKeyValuePair<StringHash32, ActorDefinition>.Key { get { return Id; } }

            ActorDefinition IKeyValuePair<StringHash32, ActorDefinition>.Value { get { return this; } }
        }

        [Serializable]
        public class BehaviorDefinition : IKeyValuePair<StringHash32, BehaviorDefinition>
        {
            public string Id;
            public string ObserveTitleId;
            public string DescId;
            public string ShortLabelId;
            public Sprite Icon;

            StringHash32 IKeyValuePair<StringHash32, BehaviorDefinition>.Key { get { return Id; } }

            BehaviorDefinition IKeyValuePair<StringHash32, BehaviorDefinition>.Value { get { return this; } }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private TankDefinition[] m_TankDefs = null;
        [SerializeField, FormerlySerializedAs("m_WaterDefs")] private EcoDefinition[] m_EcoDefs = null;
        [SerializeField] private ActorDefinition[] m_ActorDefs = null;
        [SerializeField] private BehaviorDefinition[] m_BehaviorDefs = null;

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

        public IEnumerable<EcoDefinition> AllNonEmptyEcos()
        {
            for(int i = 0; i < m_EcoDefs.Length; ++i)
            {
                if (!string.IsNullOrEmpty(m_EcoDefs[i].Id))
                    yield return m_EcoDefs[i];
            }
        }

        public EcoDefinition GetEco(StringHash32 inId)
        {
            EcoDefinition def;
            m_EcoDefs.TryGetValue(inId, out def);
            return def;
        }

        public IEnumerable<ActorDefinition> AllNonEmptyActors()
        {
            for(int i = 0; i < m_ActorDefs.Length; ++i)
            {
                if (!string.IsNullOrEmpty(m_ActorDefs[i].Id))
                    yield return m_ActorDefs[i];
            }
        }

        public ActorDefinition GetActor(StringHash32 inId)
        {
            ActorDefinition def;
            m_ActorDefs.TryGetValue(inId, out def);
            return def;
        }

        public BehaviorDefinition GetBehavior(StringHash32 inId)
        {
            BehaviorDefinition def;
            m_BehaviorDefs.TryGetValue(inId, out def);
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