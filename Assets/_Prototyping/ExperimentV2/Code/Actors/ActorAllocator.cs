using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ActorAllocator : MonoBehaviour {
        #region Types

        [Serializable]
        private class ActorInstancePool : SerializablePool<ActorInstance> {
            public long LastActiveTS = 0;
        }

        #endregion // Types

        #region Inspector

        [SerializeField, Required] private ActorDefinitions m_Definitions = null;
        [SerializeField, Required, FormerlySerializedAs("m_Pool")] private Transform m_PoolTransform = null;

        #endregion // Inspector

        private Dictionary<StringHash32, ActorInstancePool> m_PoolMap = new Dictionary<StringHash32, ActorInstancePool>(64);

        private void Start() {
            m_Definitions.ConfigureMaterials();
        }

        private void OnDestroy() {
            foreach (var def in m_Definitions.CritterDefinitions) {
                def.Prefab = null;
            }

            m_Definitions.RevertMaterials();
        }

        public void Cleanup(float minAgeSeconds) {
            long minAge = GetCurrentTS() - (long) (minAgeSeconds * TimeSpan.TicksPerSecond);
            foreach(var pool in m_PoolMap.Values) {
                if (pool.Count == 0 || pool.InUse > 0 || pool.LastActiveTS > minAge) {
                    continue;
                }

                pool.Reset();
                pool.Shrink(0);
            }
        }

        public ActorDefinition Define(StringHash32 inId) {
            return m_Definitions.FindDefinition(inId);
        }

        public void Prepare(StringHash32 inId, int inCount) {
            var pool = GetPool(inId, true);
            pool.LastActiveTS = GetCurrentTS();
            pool.Prewarm(inCount);
        }

        public ActorInstance Alloc(StringHash32 inId, Transform inRoot) {
            var pool = GetPool(inId, true);
            pool.LastActiveTS = GetCurrentTS();
            return pool.Alloc(inRoot);
        }

        public void Free(ActorInstance inActor) {
            var pool = GetPool(inActor.Definition.Id, false);
            if (pool != null) {
                pool.LastActiveTS = GetCurrentTS();
                pool.Free(inActor);
            }
        }

        public void FreeAll(StringHash32 inId) {
            var pool = GetPool(inId, false);
            if (pool != null && pool.InUse > 0) {
                pool.Reset();
                pool.LastActiveTS = GetCurrentTS();
            }
        }

        public void FreeAll() {
            foreach (var pool in m_PoolMap.Values) {
                if (pool.InUse > 0) {
                    pool.Reset();
                    pool.LastActiveTS = GetCurrentTS();
                }
            }
        }

        static private long GetCurrentTS() {
            return Stopwatch.GetTimestamp();
        }

        private ActorInstancePool GetPool(StringHash32 inId, bool inbCreate) {
            ActorInstancePool pool;
            if (!m_PoolMap.TryGetValue(inId, out pool) && inbCreate) {
                pool = new ActorInstancePool();
                ActorDefinition definition = m_Definitions.FindDefinition(inId);
                if (!definition.Prefab) {
                    definition.Prefab = Resources.Load<ActorInstance>("ExpCritters/" + definition.PrefabName);
                }

                Assert.True(definition.Prefab, "No prefab for ActorDefinition {0}", definition.Id);
                pool.Prefab = definition.Prefab;
                pool.ConfigureTransforms(m_PoolTransform, null, true);
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