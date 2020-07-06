using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Sim Database")]
    public class EnergySimDatabase : ScriptableObject, ISimDatabase
    {
        #region Inspector

        [SerializeField]
        private string m_Id = null;

        [SerializeField]
        private ActorType[] m_ActorTypes = null;

        [SerializeField]
        private EnvironmentType[] m_EnvironmentTypes = null;

        [SerializeField]
        private VarType[] m_VarTypes = null;
        
        #endregion // Inspector

        [NonSerialized] private SimTypeDatabase<ActorType> m_ActorDatabase;
        [NonSerialized] private SimTypeDatabase<EnvironmentType> m_EnvDatabase;
        [NonSerialized] private VarTypeDatabase m_VarDatabase;

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private int m_Version;
        [NonSerialized] private Action m_CachedDirtyDelegate;

        #region Initialization

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_CachedDirtyDelegate = ((ISimDatabase) this).Dirty;

            m_ActorDatabase = new SimTypeDatabase<ActorType>(m_ActorTypes);
            m_EnvDatabase = new SimTypeDatabase<EnvironmentType>(m_EnvironmentTypes);
            m_VarDatabase = new VarTypeDatabase(m_VarTypes);

            m_ActorDatabase.OnDirty += m_CachedDirtyDelegate;
            m_EnvDatabase.OnDirty += m_CachedDirtyDelegate;
            m_VarDatabase.OnDirty += m_CachedDirtyDelegate;

            m_Initialized = true;
            m_Version = 0;
        }

        #endregion // Initialization

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Dispose();
        }

        public string Id() { return m_Id; }

        public SimTypeDatabase<ActorType> Actors { get { return m_ActorDatabase; } }
        public SimTypeDatabase<EnvironmentType> Envs { get { return m_EnvDatabase; } }
        
        public VarTypeDatabase Vars { get { return m_VarDatabase; } }
        public SimTypeDatabase<VarType> Resources { get { return m_VarDatabase.Resources; } }
        public SimTypeDatabase<VarType> Properties { get { return m_VarDatabase.Properties; } }

        #region IUpdateVersioned

        [UnityEngine.Scripting.Preserve]
        int IUpdateVersioned.GetUpdateVersion()
        {
            return m_Version;
        }

        void ISimDatabase.Dirty()
        {
            UpdateVersion.Increment(ref m_Version);
        }

        #endregion // IUpdateVersioned

        #region IDisposable

        public void Dispose()
        {
            if (m_Initialized)
            {
                Ref.Dispose(ref m_ActorDatabase);
                Ref.Dispose(ref m_EnvDatabase);
                Ref.Dispose(ref m_VarDatabase);

                m_Version = 0;
                m_Initialized = false;
            }
        }

        #endregion // IDisposable
    }
}