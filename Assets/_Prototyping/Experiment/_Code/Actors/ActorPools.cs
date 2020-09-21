using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    public class ActorPools : MonoBehaviour
    {
        [Serializable]
        private class ActorPool : SerializablePool<ActorCtrl>
        {
            [NonSerialized] private StringHash m_Id;
            public StringHash Id { get { return m_Id.IsEmpty ? (m_Id = Name) : m_Id; } }
        }

        #region Inspector

        [SerializeField, EditModeOnly] private ActorPool[] m_Pools = null;
        [SerializeField, EditModeOnly] private Transform m_PoolRoot = null;

        #endregion // Inspector

        [NonSerialized] private Dictionary<StringHash, ActorPool> m_PoolMap;
        [NonSerialized] private StringHash[] m_AllIds;

        public IReadOnlyList<StringHash> AllIds()
        {
            return m_AllIds ?? (m_AllIds = ArrayUtils.MapFrom(m_Pools, (p) => p.Id));
        }

        /// <summary>
        /// Allocates an actor from the pool with the given id.
        /// </summary>
        public ActorCtrl Alloc(StringHash inId)
        {
            InitMap();

            ActorPool pool;
            if (m_PoolMap.TryGetValue(inId, out pool))
            {
                return pool.Alloc();
            }
            else
            {
                Debug.LogErrorFormat("[ActorPools] Unrecognized actor pool id '{0}'", inId.ToDebugString());
                return null;
            }
        }

        /// <summary>
        /// Resets the actor pool with the given id.
        /// </summary>
        public void Reset(StringHash inId)
        {
            if (m_PoolMap != null)
            {
                ActorPool pool;
                if (m_PoolMap.TryGetValue(inId, out pool))
                {
                    pool.Reset();
                }
                else
                {
                    Debug.LogWarningFormat("[ActorPools] Unrecognized actor pool id '{0}'", inId.ToDebugString());
                }
            }
        }

        /// <summary>
        /// Resets all actor pools.
        /// </summary>
        public void ResetAll()
        {
            foreach(var pool in m_Pools)
            {
                pool.Reset();
            }
        }

        private void InitMap()
        {
            if (m_PoolMap != null)
                return;
            
            m_PoolMap = new Dictionary<StringHash, ActorPool>(m_Pools.Length);
            foreach(var pool in m_Pools)
            {
                m_PoolMap.Add(pool.Id, pool);
                pool.ConfigureTransforms(null, m_PoolRoot, true);
            }
        }
    }
}