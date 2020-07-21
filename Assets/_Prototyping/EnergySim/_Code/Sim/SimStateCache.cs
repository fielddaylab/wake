using System.Text;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SimStateCache
    {
        public const int DefaultCacheSize = 8;

        private struct CacheEntry
        {
            public EnergySimState State;
            public ulong LastAccessOperation;

            public void Initialize()
            {
                State = new EnergySimState();
                LastAccessOperation = 0;
            }

            public void Clear()
            {
                LastAccessOperation = 0;
            }
        }

        private readonly EnergySimState m_StartingState;
        private readonly EnergySimState m_TempState;
        private CacheEntry[] m_StateCache;
        private readonly int m_CacheSize;
        
        private ulong m_OperationCount;
        private int m_AllocatedCount;

        public SimStateCache(int inCacheSize = DefaultCacheSize)
        {
            m_StartingState = new EnergySimState();
            m_TempState = new EnergySimState();

            m_CacheSize = inCacheSize;
            m_StateCache = new CacheEntry[inCacheSize];
            for(int i = 0; i < inCacheSize; ++i)
            {
                m_StateCache[i].Initialize();
            }
        }

        public EnergySimState StartingState()
        {
            return m_StartingState;
        }

        public EnergySimState TempState()
        {
            return m_TempState;
        }

        public EnergySimState Find(ushort inTick)
        {
            ++m_OperationCount;

            if (inTick == 0)
            {
                return m_StartingState;
            }

            for(int i = 0; i < m_AllocatedCount; ++i)
            {
                ref CacheEntry entry = ref m_StateCache[i];
                if (entry.State.Timestamp == inTick)
                {
                    // Debug.LogFormat("[SimStateCache] Accessed cache #{0} for tick {1}", i, inTick);
                    entry.LastAccessOperation = m_OperationCount;
                    return entry.State;
                }
            }

            return null;
        }

        public EnergySimState FindClosest(ushort inTick)
        {
            ++m_OperationCount;

            if (inTick == 0)
            {
                return m_StartingState;
            }

            int closestIdx = -1;
            int closestDistance = ushort.MaxValue;
            for(int i = 0; i < m_AllocatedCount; ++i)
            {
                ref CacheEntry entry = ref m_StateCache[i];
                if (entry.State.Timestamp == inTick)
                {
                    // Debug.LogFormat("[SimStateCache] Accessed cache #{0} for tick {1}", i, inTick);
                    entry.LastAccessOperation = m_OperationCount;
                    return entry.State;
                }
                else if (entry.State.Timestamp < inTick)
                {
                    int distance = inTick - entry.State.Timestamp;
                    if (distance < closestDistance)
                    {
                        closestIdx = i;
                        closestDistance = distance;
                    }
                }
            }

            if (closestIdx >= 0)
            {
                ref CacheEntry entry = ref m_StateCache[closestIdx];
                entry.LastAccessOperation = m_OperationCount;
                // Debug.LogFormat("[SimStateCache] Accessed cache #{0} for closest tick {1} to tick {2}", closestIdx, entry.State.Timestamp, inTick);
                return entry.State;
            }

            return m_StartingState;
        }

        public EnergySimState Store(EnergySimState inState)
        {
            ++m_OperationCount;

            if (inState.Timestamp == 0)
            {
                // Debug.LogFormat("[SimStateCache] Stored tick 0 to starting state");
                m_StartingState.CopyFrom(inState);
                return m_StartingState;
            }

            for(int i = 0; i < m_AllocatedCount; ++i)
            {
                ref CacheEntry entry = ref m_StateCache[i];
                if (entry.State.Timestamp != inState.Timestamp)
                    continue;

                // Debug.LogFormat("[SimStateCache] Stored tick {0} to cache #{1}", inState.Timestamp, i);
                entry.State.CopyFrom(inState);
                entry.LastAccessOperation = m_OperationCount;
                return entry.State;
            }

            if (m_AllocatedCount == m_CacheSize)
            {
                DiscardOldest();
            }

            ref CacheEntry nextEntry = ref m_StateCache[m_AllocatedCount++];
            nextEntry.State.CopyFrom(inState);
            nextEntry.LastAccessOperation = m_OperationCount;
            // Debug.LogFormat("[SimStateCache] Stored tick {0} to cache #{1}", inState.Timestamp, m_AllocatedCount - 1);
            return nextEntry.State;
        }

        public EnergySimState StoreTemp(EnergySimState inState)
        {
            ++m_OperationCount;
            m_TempState.CopyFrom(inState);
            return m_TempState;
        }

        public void Invalidate()
        {
            for(int i = 0; i < m_AllocatedCount; ++i)
            {
                m_StateCache[i].Clear();
            }
            m_AllocatedCount = 0;
            m_TempState.Reset();
            // Debug.LogFormat("[SimStateCache] Invalidated all caches");
        }

        public void Dump()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(m_AllocatedCount).Append("/").Append(m_CacheSize).Append(" allocated");
            for(int i = 0; i < m_AllocatedCount; ++i)
            {
                ref CacheEntry entry = ref m_StateCache[i];
                builder.Append("\n - ").Append(i).Append(": tick ").Append(entry.State.Timestamp).Append(" (age ").Append(m_OperationCount - entry.LastAccessOperation).Append(")");
            }
            Debug.Log(builder.Flush());
        }

        private bool DiscardOldest()
        {
            int oldestIdx = -1;
            ulong oldestAge = 0;

            for(int i = 0; i < m_AllocatedCount; ++i)
            {
                ref CacheEntry entry = ref m_StateCache[i];
                ulong age = m_OperationCount - entry.LastAccessOperation;
                if (oldestIdx == -1 || age > oldestAge)
                {
                    oldestIdx = i;
                    oldestAge = age;
                }
            }

            if (oldestIdx >= 0)
            {
                // Debug.LogFormat("[SimStateCache] Discarding oldest cache #{0} with tick {1}", oldestIdx, m_StateCache[oldestIdx].State.Timestamp);
                ArrayUtils.FastRemoveAt(m_StateCache, ref m_AllocatedCount, oldestIdx);
                return true;
            }

            return false;
        }
    }
}