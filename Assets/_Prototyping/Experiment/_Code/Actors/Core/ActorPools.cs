using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using System.Collections.Generic;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public class ActorPools : MonoBehaviour, ISceneUnloadHandler
    {
        [Serializable]
        private class ActorPool : SerializablePool<ActorCtrl>
        {
            [NonSerialized] private StringHash32 m_Id;
            public StringHash32 Id { get { return m_Id.IsEmpty ? (m_Id = Name) : m_Id; } }
        }

        #region Inspector

        [SerializeField, EditModeOnly] private ActorPool[] m_Pools = null;
        [SerializeField, EditModeOnly] private Transform m_PoolRoot = null;

        #endregion // Inspector

        [NonSerialized] private Dictionary<StringHash32, ActorPool> m_PoolMap;
        [NonSerialized] private StringHash32[] m_AllIds;

        public IReadOnlyList<StringHash32> AllIds()
        {
            return m_AllIds ?? (m_AllIds = ArrayUtils.MapFrom(m_Pools, (p) => p.Id));
        }

        /// <summary>
        /// Allocates an actor from the pool with the given id.
        /// </summary>
        public ActorCtrl Alloc(StringHash32 inId, Transform inTarget)
        {
            InitMap();

            ActorPool pool;
            if (m_PoolMap.TryGetValue(inId, out pool))
            {
                return pool.Alloc(inTarget);
            }
            else
            {
                Log.Error("[ActorPools] Unrecognized actor pool id '{0}'", inId);
                return null;
            }
        }

        /// <summary>
        /// Returns all currently active actors.
        /// </summary>
        public IReadOnlyList<ActorCtrl> Active(StringHash32 inId)
        {
            InitMap();

            ActorPool pool;
            if (m_PoolMap.TryGetValue(inId, out pool))
            {
                return pool.ActiveObjects;
            }
            else
            {
                Log.Error("[ActorPools] Unrecognized actor pool id '{0}'", inId);
                return null;
            }
        }

        /// <summary>
        /// Resets the actor pool with the given id.
        /// </summary>
        public void Reset(StringHash32 inId)
        {
            if (m_PoolMap != null)
            {
                ActorPool pool;
                if (m_PoolMap.TryGetValue(inId, out pool))
                {
                    pool.Reset();
                    pool.Prefab.Config.ResetBlackboard();
                }
                else
                {
                    Log.Warn("[ActorPools] Unrecognized actor pool id '{0}'", inId);
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
                pool.Prefab.Config.ResetBlackboard();
            }
        }

        private void InitMap()
        {
            if (m_PoolMap != null)
                return;
            
            m_PoolMap = new Dictionary<StringHash32, ActorPool>(m_Pools.Length);
            foreach(var pool in m_Pools)
            {
                m_PoolMap.Add(pool.Id, pool);
                pool.ConfigureTransforms(m_PoolRoot, transform, true);
            }
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext)
        {
            ResetAll();
        }
    }
}