using System;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Environment Type")]
    public class EnvironmentType : ScriptableObject, IKeyValuePair<FourCC, EnvironmentType>
    {
        #region Types

        [Serializable]
        public struct ResourceConfig
        {
            [VarTypeId] public FourCC ResourceId;
            public ushort Base;
            public ushort Random;
        }

        [Serializable]
        public struct DefaultPropertyConfig
        {
            [VarTypeId] public FourCC PropertyId;
            public float Base;
            public float Random;
        }

        #endregion // Types

        #region Inspector

        [SerializeField, EnvironmentTypeId] private FourCC m_Id = FourCC.Zero;
        [SerializeField] private string m_ScriptName = null;

        [Header("Variables")]
        [SerializeField] private ResourceConfig[] m_ResourcesPerTick = null;
        [SerializeField] private DefaultPropertyConfig[] m_DefaultProperties = null;

        [Header("Other")]
        [SerializeField] private PropertyBlock m_ExtraData = default(PropertyBlock);

        #endregion // Inspector

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, EnvironmentType>.Key { get { return m_Id; } }

        EnvironmentType IKeyValuePair<FourCC, EnvironmentType>.Value { get { return this; } }

        #endregion // KeyValuePair

        #region Accessors

        public FourCC Id() { return m_Id; }
        public string ScriptName() { return m_ScriptName; }

        public PropertyBlock ExtraData() { return m_ExtraData; }

        #endregion // Accessors

        #region Operations

        public void AddResources(ref EnvironmentState ioState, in EnergySimContext inContext)
        {
            for(int resAddIdx = 0; resAddIdx < m_ResourcesPerTick.Length; ++resAddIdx)
            {
                ResourceConfig config = m_ResourcesPerTick[resAddIdx];
                int resIdx = inContext.Database.ResourceVarToIndex(config.ResourceId);
                ioState.OwnedResources[resIdx] += (ushort) (config.Base + inContext.RNG.Next(config.Random + 1));
            }
        }

        public void DefaultProperties(ref EnvironmentState ioState, in EnergySimContext inContext)
        {
            for(int propAddIdx = 0; propAddIdx < m_DefaultProperties.Length; ++propAddIdx)
            {
                DefaultPropertyConfig config = m_DefaultProperties[propAddIdx];
                int propIdx = inContext.Database.PropertyVarToIndex(config.PropertyId);
                ioState.Properties[propIdx] = config.Base + inContext.RNG.NextFloat(-config.Random, config.Random);
            }
        }

        #endregion // Operations
    }
}