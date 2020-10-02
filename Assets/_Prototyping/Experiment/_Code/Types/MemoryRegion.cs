using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    /// <summary>
    /// Memory storage region.
    /// </summary>
    [Serializable]
    public class MemoryRegion
    {
        [SerializeField, Range(0, 15), EditModeOnly, Tooltip("Maximum number of memories to keep at a given time")]
        private int m_Capacity = 4;

        [SerializeField, Range(0, 10), Tooltip("Minimum intensity for a memory to be stored")]
        private float m_MinIntensity = 0;

        [SerializeField, Range(0, 10), Tooltip("Maximum intensity for a memory to be stored")]
        private float m_MaxIntensity = 10;

        private RingBuffer<Memory> m_Entries;

        #region Operations

        /// <summary>
        /// Resets the memory region.
        /// </summary>
        public void Reset()
        {
            if (m_Entries != null)
            {
                m_Entries.Clear();
                m_Entries.SetCapacity(m_Capacity);
            }
            else
            {
                m_Entries = new RingBuffer<Memory>(m_Capacity, RingBufferMode.Fixed);
            }
        }

        /// <summary>
        /// Attempts to store a memory.
        /// </summary>
        public bool TryStore(StringHash32 inId, float inIntensity, uint inCurrentTime, TimeSpan inDuration = default(TimeSpan), StringHash32 inTag = default(StringHash32), Vector2? inLocation = null, float inPrecision = 0)
        {
            if (inIntensity < m_MinIntensity || inIntensity > m_MaxIntensity)
                return false;

            int existingIdx = IndexOf(inId);
            if (existingIdx >= 0)
            {
                ref Memory entryToUpdate = ref m_Entries[existingIdx];
                if (entryToUpdate.Timestamp > inCurrentTime)
                    return false;

                entryToUpdate.Intensity = inIntensity;
                entryToUpdate.Timestamp = inCurrentTime;
                entryToUpdate.Expiration = inDuration.Milliseconds == 0 ? 0 : inCurrentTime + (uint) inDuration.Milliseconds;

                entryToUpdate.Tag = inTag;
                entryToUpdate.Location = inLocation;
                entryToUpdate.Precision = inPrecision;
                return true;
            }

            if (m_Entries.IsFull())
            {
                if (DiscardExpiredMemories(inCurrentTime) == 0)
                {
                    DiscardOldestIfNecessary();
                }
            }

            Memory newEntry;
            newEntry.Id = inId;
            newEntry.Intensity = inIntensity;
            newEntry.Timestamp = inCurrentTime;
            newEntry.Expiration = inDuration.Milliseconds == 0 ? 0 : inCurrentTime + (uint) inDuration.Milliseconds;
            newEntry.Tag = inTag;
            newEntry.Location = inLocation;
            newEntry.Precision = inPrecision;
            m_Entries.PushFront(newEntry);
            return true;
        }

        /// <summary>
        /// Refreshes an existing memory and potentially extends its expiration time.
        /// </summary>
        public bool RefreshMemory(StringHash32 inId, uint inCurrentTime, uint inDuration)
        {
            int existingIdx = IndexOf(inId);
            if (existingIdx >= 0)
            {
                ref Memory entry = ref m_Entries[existingIdx];
                if (IsExpired(entry, inCurrentTime))
                {
                    m_Entries.FastRemoveAt(existingIdx);
                    return false;
                }

                ulong timeRemaining = entry.Expiration == 0 ? 0 : entry.Expiration - inCurrentTime;
                if (inDuration > timeRemaining)
                {
                    entry.Timestamp = inCurrentTime;
                    entry.Expiration = inCurrentTime + inDuration;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to recall a memory by id.
        /// </summary>
        public bool TryRecallById(StringHash32 inId, uint inCurrentTime, out Memory outMemory)
        {
            int existingIdx = IndexOf(inId);
            if (existingIdx >= 0)
            {
                ref Memory entry = ref m_Entries[existingIdx];
                if (IsExpired(entry, inCurrentTime))
                {
                    m_Entries.FastRemoveAt(existingIdx);
                    outMemory = default(Memory);
                    return false;
                }

                outMemory = entry;
                return true;
            }

            outMemory = default(Memory);
            return false;
        }

        /// <summary>
        /// Attempts to recall the most intense memory with the given tag.
        /// </summary>
        public bool TryRecallMostIntenseByTag(StringHash32 inTag, uint inCurrentTime, out Memory outMemory)
        {
            int intenseIdx = -1;
            float intensity = float.MinValue;

            for (int i = m_Entries.Count - 1; i >= 0; --i)
            {
                ref Memory entry = ref m_Entries[i];
                if (entry.Tag != inTag)
                    continue;

                if (IsExpired(entry, inCurrentTime))
                {
                    m_Entries.FastRemoveAt(i);
                    continue;
                }

                if (entry.Intensity > intensity)
                {
                    intenseIdx = i;
                    intensity = entry.Intensity;
                }
            }

            if (intenseIdx >= 0)
            {
                outMemory = m_Entries[intenseIdx];
                return true;
            }

            outMemory = default(Memory);
            return false;
        }

        /// <summary>
        /// Attempts to recall all memories with the given tag.
        /// </summary>
        public int RecallAllByTag<TCollection>(StringHash32 inTag, uint inCurrentTime, ref TCollection outMemories)
            where TCollection : ICollection<Memory>
        {
            int recalled = 0;

            for (int i = m_Entries.Count - 1; i >= 0; --i)
            {
                ref Memory entry = ref m_Entries[i];
                if (entry.Tag != inTag)
                    continue;

                if (IsExpired(entry, inCurrentTime))
                {
                    m_Entries.FastRemoveAt(i);
                    continue;
                }

                ++recalled;
                outMemories.Add(entry);
            }

            return recalled;
        }

        /// <summary>
        /// Forgets the memory with the given id.
        /// </summary>
        public bool Forget(StringHash32 inId)
        {
            int existingIdx = IndexOf(inId);
            if (existingIdx >= 0)
            {
                m_Entries.FastRemoveAt(existingIdx);
                return true;
            }

            return false;
        }

        #endregion // Operations

        #region Internal

        private bool IsExpired(in Memory inEntry, uint inCurrentTime)
        {
            return inEntry.Expiration > 0 && inEntry.Expiration <= inCurrentTime;
        }

        private int DiscardExpiredMemories(uint inCurrentTime)
        {
            int expired = 0;

            for (int i = m_Entries.Count - 1; i >= 0; --i)
            {
                if (IsExpired(m_Entries[i], inCurrentTime))
                {
                    m_Entries.FastRemoveAt(i);
                    ++expired;
                }
            }

            return expired;
        }

        private void DiscardOldestIfNecessary()
        {
            int oldestIdx = -1;
            ulong oldestTS = ulong.MaxValue;

            for (int i = m_Entries.Count - 1; i >= 0; --i)
            {
                ref Memory entry = ref m_Entries[i];
                if (entry.Timestamp < oldestTS)
                {
                    oldestIdx = i;
                    oldestTS = entry.Timestamp;
                }
            }

            if (oldestIdx >= 0)
            {
                m_Entries.FastRemoveAt(oldestIdx);
            }
        }

        private int IndexOf(StringHash32 inId)
        {
            for (int i = 0; i < m_Entries.Count; ++i)
            {
                if (m_Entries[i].Id == inId)
                    return i;
            }

            return -1;
        }

        #endregion // Internal
    }
}