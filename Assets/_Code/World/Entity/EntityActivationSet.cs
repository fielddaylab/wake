using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Entity {
    public sealed class EntityActivationSet<TEntity, TUpdateContext>
        where TEntity : UnityEngine.Object, IActiveEntity
        where TUpdateContext : struct
    {
        #region Types

        public delegate bool SetStatusDelegate(TEntity entity, EntityActiveStatus status, bool force);
        public delegate UpdateAwakeResult UpdateAwakeDelegate(TEntity entity, in TUpdateContext context);
        public delegate void UpdateActiveDelegate(TEntity entity, in TUpdateContext context);

        [Flags]
        private enum ListModifiedFlags {
            Sleeping = 0x01,
            Awake = 0x02,
            Active = 0x04
        }

        #endregion // Types

        #region State

        [NonSerialized] private int m_LastUpdateMask;

        // everything sleeping
        private readonly RingBuffer<TEntity> m_Sleeping;
        
        // everything not sleeping
        private readonly RingBuffer<TEntity> m_Awake;
        
        // everything active
        private readonly RingBuffer<TEntity> m_Active;
        
        // all entities
        private readonly HashSet<TEntity> m_All;

        [NonSerialized] private bool m_ForceSleep = false;
        [NonSerialized] private ListModifiedFlags m_DirtyLists;

        #endregion // State

        public EntityActivationSet(int capacity) {
            m_Sleeping = new RingBuffer<TEntity>(capacity, RingBufferMode.Expand);
            m_Awake = new RingBuffer<TEntity>(capacity / 2, RingBufferMode.Expand);
            m_Active = new RingBuffer<TEntity>(capacity / 4, RingBufferMode.Expand);
            m_All = Collections.NewSet<TEntity>(capacity);
        }

        #region Callbacks

        /// <summary>
        /// Sets the status of the object.
        /// </summary>
        public SetStatusDelegate SetStatus;

        /// <summary>
        /// Updates the given awake object.
        /// </summary>
        public UpdateAwakeDelegate UpdateAwake;

        /// <summary>
        /// Updates the given active object.
        /// </summary>
        public UpdateActiveDelegate UpdateActive;

        #endregion // Callbacks

        /// <summary>
        /// Set of all active entities.
        /// </summary>
        public ListSlice<TEntity> AllActive { get { return m_Active; } }
        
        /// <summary>
        /// Set of all awake entities.
        /// </summary>
        public ListSlice<TEntity> AllAwake { get { return m_Awake; } }

        /// <summary>
        /// Updates the entity set.
        /// </summary>
        public void Update(int updateMask, TUpdateContext args) {
            if (m_ForceSleep) {
                return;
            }

            HandleUpdateMaskChanged(updateMask);
            HandleAwakeUpdate(args);
            HandleActiveUpdate(args);
        }

        private void HandleAwakeUpdate(in TUpdateContext args) {
             m_DirtyLists &= ~ListModifiedFlags.Awake;

            TEntity entity;
            for(int i = 0, length = m_Awake.Count; i < length; i++) {
                entity = m_Awake[i];
                bool wasActive = (entity.ActiveStatus & EntityActiveStatus.Active) != 0;
                UpdateAwakeResult result = UpdateAwake(entity, args);
                if (result == UpdateAwakeResult.Skip) {
                    continue;
                }

                bool nowActive = result == UpdateAwakeResult.Active;
                if (nowActive && !wasActive) {
                    m_Active.PushBack(entity);
                    SetStatus(entity, entity.ActiveStatus | EntityActiveStatus.Active, false);
                    m_DirtyLists |= ListModifiedFlags.Active;
                } else if (wasActive && !nowActive) {
                    // m_Active.FastRemove(entity); // defer this to the active processing step
                    SetStatus(entity, entity.ActiveStatus & ~EntityActiveStatus.Active, false);
                    m_DirtyLists |= ListModifiedFlags.Active;
                }
            }
        }

        private void HandleActiveUpdate(in TUpdateContext args) {
            if (UpdateActive != null) {
                m_DirtyLists &= ~ListModifiedFlags.Active;

                TEntity entity;
                for(int i = 0, length = m_Active.Count; i < length; i++) {
                    entity = m_Active[i];
                    if (entity.ActiveStatus != EntityActiveStatus.AwakeAndActive) {
                        m_Active.FastRemoveAt(i);
                        i--;
                        length--;
                        continue;
                    }
                    UpdateActive(m_Active[i], args);
                }
            }
        }

        private void HandleUpdateMaskChanged(int newMask) {
            if (newMask == m_LastUpdateMask) {
                return;
            }

            m_LastUpdateMask = newMask;

            int awakeCount = m_Awake.Count;
            int sleepCount = m_Sleeping.Count;

            TEntity entity;
            for(int i = m_Active.Count - 1; i >= 0; i--) {
                entity = m_Active[i];
                if ((entity.UpdateMask & newMask) == 0) {
                    m_Active.FastRemoveAt(i);
                    m_DirtyLists |= ListModifiedFlags.Active;
                }
            }

            for(int i = awakeCount - 1; i >= 0; i--) {
                entity = m_Awake[i];
                if ((entity.UpdateMask & newMask) == 0) {
                    m_Sleeping.PushBack(entity);
                    m_Awake.FastRemoveAt(i);
                    SetStatus(entity, EntityActiveStatus.Sleeping, false);
                    m_DirtyLists |= ListModifiedFlags.Awake | ListModifiedFlags.Sleeping;
                }
            }

            for(int i = sleepCount - 1; i >= 0; i--) {
                entity = m_Sleeping[i];
                if ((entity.UpdateMask & newMask) != 0) {
                    m_Awake.PushBack(entity);
                    m_Sleeping.FastRemoveAt(i);
                    SetStatus(entity, EntityActiveStatus.Awake, false);
                    m_DirtyLists |= ListModifiedFlags.Sleeping | ListModifiedFlags.Awake;
                }
            }
        }

        /// <summary>
        /// Tracks the given entity.
        /// </summary>
        public void Track(TEntity entity) {
            if (!m_All.Add(entity)) {
                return;
            }

            if (!m_ForceSleep && (entity.UpdateMask & m_LastUpdateMask) != 0) {
                m_Awake.PushBack(entity);
                m_DirtyLists |= ListModifiedFlags.Awake;
                SetStatus(entity, EntityActiveStatus.Awake, true);
            } else {
                m_Sleeping.PushBack(entity);
                m_DirtyLists |= ListModifiedFlags.Sleeping;
                SetStatus(entity, EntityActiveStatus.Sleeping, true);
            }
        }

        /// <summary>
        /// Stops tracking the given entity.
        /// </summary>
        public void Untrack(TEntity entity) {
            if (!m_All.Remove(entity)) {
                return;
            }

            if (entity.ActiveStatus == EntityActiveStatus.Sleeping) {
                m_Sleeping.FastRemove(entity);
                m_DirtyLists |= ListModifiedFlags.Sleeping;
                return;
            }

            if ((entity.ActiveStatus & EntityActiveStatus.Active) != 0) {
                m_Active.FastRemove(entity);
                m_DirtyLists |= ListModifiedFlags.Active;
            }

            if ((entity.ActiveStatus & EntityActiveStatus.Awake) != 0) {
                m_Awake.FastRemove(entity);
                m_DirtyLists |= ListModifiedFlags.Awake;
            }

            SetStatus(entity, EntityActiveStatus.Sleeping, true);
        }
    
        /// <summary>
        /// Forces all entities to sleep.
        /// </summary>
        public void Sleep() {
            if (m_ForceSleep) {
                return;
            }

            m_LastUpdateMask = 0;
            m_ForceSleep = true;

            TEntity entity;
            for(int i = m_Awake.Count - 1; i >= 0; i--) {
                entity = m_Awake[i];
                m_Sleeping.PushBack(entity);
                SetStatus(entity, EntityActiveStatus.Sleeping, true);
            }

            m_Awake.Clear();
            m_Active.Clear();
        }

        /// <summary>
        /// Cancels the last Sleep call, allowing objects to update.
        /// </summary>
        public void WakeUp() {
            m_ForceSleep = false;
        }
    }

    /// <summary>
    /// Interface for an active entity.
    /// </summary>
    public interface IActiveEntity : IBatchId {
        int UpdateMask { get; }
        EntityActiveStatus ActiveStatus { get; }
    }

    /// <summary>
    /// Active status flags for an entity.
    /// </summary>
    [Flags]
    public enum EntityActiveStatus : byte {
        Sleeping = 0,
        Awake = 0x01,
        Active = 0x02,

        [Hidden] AwakeAndActive = Awake | Active
    }

    /// <summary>
    /// Result of an awake update.
    /// </summary>
    public enum UpdateAwakeResult {
        Inactive,
        Active,
        Skip
    }
}