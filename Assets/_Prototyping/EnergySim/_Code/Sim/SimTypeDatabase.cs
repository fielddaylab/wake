using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class SimTypeDatabase<T> : IDisposable where T : ISimType<T>
    {
        protected T[] m_Types;
        protected Dictionary<FourCC, T> m_IdMap;
        protected FourCC[] m_Ids;
        protected Dictionary<string, FourCC> m_ScriptNameMap;

        public event Action OnDirty;

        public SimTypeDatabase(IReadOnlyList<T> inTypes)
        {
            m_Types = new T[inTypes.Count];
            for(int i = 0; i < m_Types.Length; ++i)
            {
                m_Types[i] = inTypes[i];
            }

            ConstructLookups();
        }

        public SimTypeDatabase(T[] inSource)
        {
            m_Types = inSource;
            ConstructLookups();
        }

        public virtual void Dispose()
        {
            OnDirty = null;

            if (m_Types != null)
            {
                for(int i = m_Types.Length - 1; i >= 0; --i)
                {
                    m_Types[i].Unhook(this);
                }

                m_Types = null;
            }

            m_IdMap = null;
            m_Ids = null;
            m_ScriptNameMap = null;
        }

        protected virtual void ConstructLookups()
        {
            m_IdMap = new Dictionary<FourCC, T>(m_Types.Length);
            m_Ids = new FourCC[m_Types.Length];
            m_ScriptNameMap = new Dictionary<string, FourCC>(m_Types.Length);

            for(int i = 0; i < m_Types.Length; ++i)
            {
                T type = m_Types[i];

                FourCC id = type.Id();
                m_IdMap[id] = type;
                m_Ids[i] = id;
                m_ScriptNameMap[type.ScriptName()] = id;

                type.Hook(this);
            }
        }

        internal void Dirty()
        {
            OnDirty?.Invoke();
        }

        public int Count() { return m_Types.Length; }
        public T[] Types() { return m_Types; }
        public FourCC[] Ids() { return m_Ids; }

        #region Id Resolution
        
        public int IdToIndex(FourCC inId)
        {
            return Array.IndexOf(m_Ids, inId);
        }

        public FourCC IndexToId(int inIndex)
        {
            return m_Ids[inIndex];
        }

        public FourCC ScriptNameToId(string inScriptName)
        {
            FourCC id;
            m_ScriptNameMap.TryGetValue(inScriptName, out id);
            return id;
        }

        #endregion // Id Resolution

        #region Retrieval

        public T Get(FourCC inId)
        {
            return m_IdMap[inId];
        }

        public T Get(int inIndex)
        {
            return m_Types[inIndex];
        }

        #endregion // Retrieval
    }
}