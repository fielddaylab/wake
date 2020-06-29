using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SimDatabaseOverride : ISimDatabase
    {
        private EnergySimDatabase m_Source;
        private SimTypeDatabase<ActorType> m_ActorOverrides;
        
        private Action m_CachedDirtyDelegate;
        private int m_Version;

        public SimDatabaseOverride(EnergySimDatabase inSimDatabase)
        {
            m_Source = inSimDatabase;

            ActorType[] overrideActorTypes = new ActorType[m_Source.Actors.Count()];
            for(int i = 0; i < overrideActorTypes.Length; ++i)
            {
                overrideActorTypes[i] = m_Source.Actors.Get(i).Clone();
            }

            m_ActorOverrides = new SimTypeDatabase<ActorType>(overrideActorTypes);

            m_CachedDirtyDelegate = this.Dirty;

            m_Source.Envs.OnDirty += m_CachedDirtyDelegate;
            m_Source.Vars.OnDirty += m_CachedDirtyDelegate;
        }

        #region ISimDatabase

        public SimTypeDatabase<ActorType> Actors { get { return m_ActorOverrides ?? m_Source.Actors; } }
        public SimTypeDatabase<EnvironmentType> Envs { get { return m_Source.Envs; } }
        
        public VarTypeDatabase Vars { get { return m_Source.Vars; } }
        public SimTypeDatabase<VarType> Resources { get { return m_Source.Resources; } }
        public SimTypeDatabase<VarType> Properties { get { return m_Source.Properties; } }

        #endregion // ISimDatabase

        #region IUpdateVersioned

        int IUpdateVersioned.GetUpdateVersion()
        {
            return m_Version;
        }

        public void Dirty()
        {
            UpdateVersion.Increment(ref m_Version);
        }

        #endregion // IUpdateVersioned

        #region IDisposable

        public void Dispose()
        {
            if (m_ActorOverrides != null)
            {
                foreach(var type in m_ActorOverrides.Types())
                {
                    ActorType.DestroyImmediate(type);
                }
                
                m_ActorOverrides.OnDirty -= m_CachedDirtyDelegate;
                Ref.Dispose(ref m_ActorOverrides);
            }

            if (m_Source != null)
            {
                if (m_Source.Envs != null)
                {
                    m_Source.Envs.OnDirty -= m_CachedDirtyDelegate;
                }

                if (m_Source.Vars != null)
                {
                    m_Source.Vars.OnDirty -= m_CachedDirtyDelegate;
                }

                m_Source = null;
            }
        }

        #endregion // IDisposable
    }
}