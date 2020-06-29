using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class VarTypeDatabase : SimTypeDatabase<VarType>
    {
        private SimTypeDatabase<VarType> m_Resources;
        private SimTypeDatabase<VarType> m_Properties;

        public VarTypeDatabase(VarType[] inTypes)
            : base(inTypes)
        {
        }

        public override void Dispose()
        {
            base.Dispose();

            Ref.Dispose(ref m_Resources);
            Ref.Dispose(ref m_Properties);
        }

        protected override void ConstructLookups()
        {
            base.ConstructLookups();

            using(PooledList<VarType> resources = PooledList<VarType>.Create())
            using(PooledList<VarType> properties = PooledList<VarType>.Create())
            {
                for(int i = 0, len = m_Types.Length; i < len; ++i)
                {
                    VarType type = m_Types[i];
                    switch(type.CalcType())
                    {
                        case VarCalculationType.Resource:
                            resources.Add(type);
                            break;

                        default:
                            properties.Add(type);
                            break;
                    }
                }

                m_Resources = new SimTypeDatabase<VarType>(resources);
                m_Properties = new SimTypeDatabase<VarType>(properties);

                m_Resources.OnDirty += Dirty;
                m_Properties.OnDirty += Dirty;
            }
        }

        public SimTypeDatabase<VarType> Resources { get { return m_Resources; } }
        public SimTypeDatabase<VarType> Properties { get { return m_Properties; } }
    }
}