using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using BeauPools;
using Aqua.Animation;
using System.Collections.Generic;
using BeauUtil.Debugger;

namespace ProtoAqua.ExperimentV2
{
    public sealed class ActorAllocator : MonoBehaviour
    {
        #region Types

        private class ActorInstancePool : SerializablePool<ActorInstance> { }

        #endregion // Types

        #region Inspector

        [SerializeField, Required] private ActorDefinitions m_Definitions = null;
        [SerializeField, Required] private Transform m_Pool = null;

        #endregion // Inspector

        private Dictionary<StringHash32, ActorInstancePool> m_PoolMap = new Dictionary<StringHash32, ActorInstancePool>(64);

        public ActorDefinition Define(StringHash32 inId)
        {
            return m_Definitions.FindDefinition(inId);
        }

        public ActorInstance Alloc(StringHash32 inId, Transform inRoot)
        {
            return GetPool(inId, true).Alloc(inRoot);
        }

        public void Free(ActorInstance inActor)
        {
            GetPool(inActor.Definition.Id, false)?.Free(inActor);
        }

        public void FreeAll(StringHash32 inId)
        {
            GetPool(inId, false)?.Reset();
        }

        public void FreeAll()
        {
            foreach(var pool in m_PoolMap.Values)
                pool.Reset();
        }

        private ActorInstancePool GetPool(StringHash32 inId, bool inbCreate)
        {
            ActorInstancePool pool;
            if (!m_PoolMap.TryGetValue(inId, out pool) && inbCreate)
            {
                pool = new ActorInstancePool();
                ActorDefinition definition = m_Definitions.FindDefinition(inId);
                
                Assert.NotNull(definition.Prefab, "No prefab for ActorDefinition {0}", definition.Id);
                pool.Prefab = definition.Prefab;
                pool.ConfigureTransforms(m_Pool, null, true);
                pool.ConfigureCapacity(16, 1, false);
                pool.Initialize(null, null, 0);
                pool.Config.RegisterOnConstruct((p, o) => o.Definition = definition);
                pool.Prewarm(1);
                m_PoolMap.Add(inId, pool);
            }
            return pool;
        }
    }
}