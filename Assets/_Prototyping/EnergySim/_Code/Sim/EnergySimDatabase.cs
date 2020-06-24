using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Sim Database")]
    public class EnergySimDatabase : ScriptableObject
    {
        #region Inspector

        [SerializeField]
        private ActorType[] m_ActorTypes = null;

        [SerializeField]
        private EnvironmentType[] m_EnvironmentTypes = null;

        [SerializeField]
        private VarType[] m_VarTypes = null;
        
        #endregion // Inspector

        [NonSerialized] private Dictionary<FourCC, ActorType> m_ActorMap;
        [NonSerialized] private FourCC[] m_ActorIds;
        [NonSerialized] private Dictionary<string, FourCC> m_ActorTypeScriptNames;

        [NonSerialized] private Dictionary<FourCC, EnvironmentType> m_EnvMap;
        [NonSerialized] private FourCC[] m_EnvIds;
        [NonSerialized] private Dictionary<string, FourCC> m_EnvTypeScriptNames;

        [NonSerialized] private Dictionary<FourCC, VarType> m_VarMap;
        [NonSerialized] private FourCC[] m_ResourceIds;
        [NonSerialized] private FourCC[] m_PropertyIds;
        [NonSerialized] private Dictionary<string, FourCC> m_VarTypeScriptNames;

        [NonSerialized] private bool m_Initialized;

        #region Initialization

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_ActorMap = KeyValueUtils.CreateMap<FourCC, ActorType, ActorType>(m_ActorTypes);
            m_ActorIds = new FourCC[m_ActorTypes.Length];
            m_ActorTypeScriptNames = new Dictionary<string, FourCC>(m_ActorTypes.Length, StringComparer.Ordinal);
            for(int i = m_ActorIds.Length - 1; i >= 0; --i)
            {
                ActorType type = m_ActorTypes[i];
                m_ActorIds[i] = type.Id();
                m_ActorTypeScriptNames[type.ScriptName()] = type.Id();
            }

            m_EnvMap = KeyValueUtils.CreateMap<FourCC, EnvironmentType, EnvironmentType>(m_EnvironmentTypes);
            m_EnvIds = new FourCC[m_EnvironmentTypes.Length];
            m_EnvTypeScriptNames = new Dictionary<string, FourCC>(m_EnvironmentTypes.Length, StringComparer.Ordinal);
            for(int i = m_EnvIds.Length - 1; i >= 0; --i)
            {
                EnvironmentType type = m_EnvironmentTypes[i];
                m_EnvIds[i] = type.Id();
                m_EnvTypeScriptNames[type.ScriptName()] = type.Id();
            }

            m_VarMap = KeyValueUtils.CreateMap<FourCC, VarType, VarType>(m_VarTypes);
            m_VarTypeScriptNames = new Dictionary<string, FourCC>(m_VarTypes.Length, StringComparer.Ordinal);
            using(PooledList<FourCC> resourceIds = PooledList<FourCC>.Create())
            using(PooledList<FourCC> propertyIds = PooledList<FourCC>.Create())
            {
                for(int i = 0, len = m_VarTypes.Length; i < len; ++i)
                {
                    VarType type = m_VarTypes[i];
                    m_VarTypeScriptNames[type.ScriptName()] = type.Id();

                    switch(type.CalcType())
                    {
                        case VarCalculationType.Resource:
                            resourceIds.Add(type.Id());
                            break;

                        case VarCalculationType.Derived:
                        case VarCalculationType.Extern:
                            propertyIds.Add(type.Id());
                            break;
                    }
                }

                m_ResourceIds = resourceIds.ToArray();
                m_PropertyIds = propertyIds.ToArray();
            }

            m_Initialized = true;
        }

        #endregion // Initialization

        #region Actors

        public int ActorTypeCount() { return m_ActorTypes.Length; }

        public FourCC[] ActorTypeIds()
        {
            if (!m_Initialized)
                Initialize();
            return m_ActorIds;
        }

        public int ActorTypeToIndex(FourCC inType)
        {
            if (!m_Initialized)
                Initialize();
            return Array.IndexOf(m_ActorIds, inType);
        }

        public FourCC ActorScriptNameToType(string inScriptName)
        {
            if (!m_Initialized)
                Initialize();
            
            FourCC type;
            m_ActorTypeScriptNames.TryGetValue(inScriptName, out type);
            return type;
        }

        public ActorType ActorType(FourCC inActorTypeId)
        {
            if (!m_Initialized)
                Initialize();
            return m_ActorMap[inActorTypeId];
        }

        public ActorType ActorType(int inActorIndex)
        {
            return m_ActorTypes[inActorIndex];
        }

        #endregion // Actors

        #region Environments

        public int EnvironmentTypeCount() { return m_EnvironmentTypes.Length; }

        public FourCC[] EnvironmentTypeIds()
        {
            if (!m_Initialized)
                Initialize();
            return m_EnvIds;
        }

        public int EnvironmentTypeToIndex(FourCC inType)
        {
            if (!m_Initialized)
                Initialize();
            return Array.IndexOf(m_EnvironmentTypes, inType);
        }

        public FourCC EnvironmentScriptNameToType(string inScriptName)
        {
            if (!m_Initialized)
                Initialize();

            FourCC type;
            m_EnvTypeScriptNames.TryGetValue(inScriptName, out type);
            return type;
        }

        public EnvironmentType EnvironmentType(FourCC inEnvironmentTypeId)
        {
            if (!m_Initialized)
                Initialize();
            return m_EnvMap[inEnvironmentTypeId];
        }

        public EnvironmentType EnvironmentType(int inEnvironmentIndex)
        {
            return m_EnvironmentTypes[inEnvironmentIndex];
        }

        #endregion // Environments
    
        #region Variables

        public int VarCount() { return m_VarTypes.Length; }
        
        public int ResourceTypeCount()
        {
            if (!m_Initialized)
                Initialize();
            return m_ResourceIds.Length;
        }

        public int PropertyTypeCount()
        {
            if (!m_Initialized)
                Initialize();
            return m_PropertyIds.Length;
        }

        public FourCC[] ResourceVarIds()
        {
            if (!m_Initialized)
                Initialize();
            return m_ResourceIds;
        }

        public FourCC[] PropertyVarIds()
        {
            if (!m_Initialized)
                Initialize();
            return m_PropertyIds;
        }

        public int ResourceVarToIndex(FourCC inResourceId)
        {
            if (!m_Initialized)
                Initialize();
            return Array.IndexOf(m_ResourceIds, inResourceId);
        }

        public int PropertyVarToIndex(FourCC inPropertyId)
        {
            if (!m_Initialized)
                Initialize();
            return Array.IndexOf(m_PropertyIds, inPropertyId);
        }

        public VarType VarType(FourCC inVarTypeId)
        {
            if (!m_Initialized)
                Initialize();
            return m_VarMap[inVarTypeId];
        }

        public VarType ResourceType(int inIndex)
        {
            if (!m_Initialized)
                Initialize();
            return m_VarMap[m_ResourceIds[inIndex]];
        }

        public VarType PropertyType(int inIndex)
        {
            if (!m_Initialized)
                Initialize();
            return m_VarMap[m_ResourceIds[inIndex]];
        }

        public FourCC VarScriptNameToType(string inScriptName)
        {
            if (!m_Initialized)
                Initialize();

            FourCC type;
            m_VarTypeScriptNames.TryGetValue(inScriptName, out type);
            return type;
        }

        #endregion // Variables
    }
}