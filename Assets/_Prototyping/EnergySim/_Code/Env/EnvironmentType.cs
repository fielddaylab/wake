using System;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Environment Type")]
    public class EnvironmentType : ScriptableObject, ISimType<EnvironmentType>, IKeyValuePair<FourCC, EnvironmentType>
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

        [NonSerialized] private SimTypeDatabase<EnvironmentType> m_Database;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, EnvironmentType>.Key { get { return m_Id; } }

        EnvironmentType IKeyValuePair<FourCC, EnvironmentType>.Value { get { return this; } }

        #endregion // KeyValuePair

        #region ISimType

        void ISimType<EnvironmentType>.Hook(SimTypeDatabase<EnvironmentType> inDatabase)
        {
            m_Database = inDatabase;
        }

        void ISimType<EnvironmentType>.Unhook(SimTypeDatabase<EnvironmentType> inDatabase)
        {
            if (m_Database == inDatabase)
            {
                m_Database = null;
            }
        }

        #endregion // ISimType

        #region Accessors

        public FourCC Id() { return m_Id; }
        public string ScriptName() { return m_ScriptName; }

        public PropertyBlock ExtraData() { return m_ExtraData; }

        #endregion // Accessors

        #region Operations

        public void AddResources(ref EnvironmentState ioState, in EnergySimContext inContext, in System.Random inRandom)
        {
            for(int resAddIdx = 0; resAddIdx < m_ResourcesPerTick.Length; ++resAddIdx)
            {
                ResourceConfig config = m_ResourcesPerTick[resAddIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(config.ResourceId);
                ioState.OwnedResources[resIdx] += (ushort) (config.Base + inRandom.Next(config.Random + 1));
            }
        }

        public void DefaultProperties(ref EnvironmentState ioState, in EnergySimContext inContext, in System.Random inRandom)
        {
            for(int propAddIdx = 0; propAddIdx < m_DefaultProperties.Length; ++propAddIdx)
            {
                DefaultPropertyConfig config = m_DefaultProperties[propAddIdx];
                int propIdx = inContext.Database.Properties.IdToIndex(config.PropertyId);
                ioState.Properties[propIdx] = config.Base + inRandom.NextFloat(-config.Random, config.Random);
            }
        }

        /// <summary>
        /// Sets this EnvironmentType configuration as dirty.
        /// </summary>
        public void Dirty()
        {
            m_Database?.Dirty();
        }

        #endregion // Operations

        #region Unity Events

        #if UNITY_EDITOR

        private void OnValidate()
        {
            Dirty();
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    }
}