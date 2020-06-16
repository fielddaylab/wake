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

        private Dictionary<FourCC, ActorType> m_ActorMap;
        [NonSerialized] private FourCC[] m_ActorIds;

        private Dictionary<FourCC, EnvironmentType> m_EnvMap;
        [NonSerialized] private FourCC[] m_EnvIds;

        private Dictionary<FourCC, VarType> m_VarMap;
        [NonSerialized] private FourCC[] m_ResourceIds;
        [NonSerialized] private FourCC[] m_PropertyIds;

        [NonSerialized] private bool m_Initialized;

        #region Initialization

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_ActorMap = KeyValueUtils.CreateMap<FourCC, ActorType, ActorType>(m_ActorTypes);
            m_ActorIds = new FourCC[m_ActorTypes.Length];
            for(int i = m_ActorIds.Length - 1; i >= 0; --i)
            {
                m_ActorIds[i] = m_ActorTypes[i].Id();
            }

            m_EnvMap = KeyValueUtils.CreateMap<FourCC, EnvironmentType, EnvironmentType>(m_EnvironmentTypes);
            m_EnvIds = new FourCC[m_EnvironmentTypes.Length];
            for(int i = m_EnvIds.Length - 1; i >= 0; --i)
            {
                m_EnvIds[i] = m_EnvironmentTypes[i].Id();
            }

            m_VarMap = KeyValueUtils.CreateMap<FourCC, VarType, VarType>(m_VarTypes);
            using(PooledList<FourCC> resourceIds = PooledList<FourCC>.Create())
            using(PooledList<FourCC> propertyIds = PooledList<FourCC>.Create())
            {
                for(int i = 0, len = m_VarTypes.Length; i < len; ++i)
                {
                    switch(m_VarTypes[i].CalcType())
                    {
                        case VarCalculationType.Resource:
                            resourceIds.Add(m_VarTypes[i].Id());
                            break;

                        case VarCalculationType.Derived:
                        case VarCalculationType.Extern:
                            propertyIds.Add(m_VarTypes[i].Id());
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
        
        public int ResourceCount()
        {
            if (!m_Initialized)
                Initialize();
            return m_ResourceIds.Length;
        }

        public int PropertyCount()
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

        #endregion // Variables
    }
}