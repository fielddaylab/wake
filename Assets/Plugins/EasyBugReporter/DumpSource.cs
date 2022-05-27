using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace EasyBugReporter {
    /// <summary>
    /// Source for a data dump.
    /// </summary>
    public interface IDumpSource {
        bool Dump(IDumpWriter dump);
    }

    /// <summary>
    /// Data dump system, with specific lifecycle methods.
    /// </summary>
    public interface IDumpSystem : IDumpSource {
        void Initialize();
        void Freeze();
        void Unfreeze();
        void Shutdown();
    }

    /// <summary>
    /// Collection of data dump sources.
    /// </summary>
    public class DumpSourceCollection : ICollection<IDumpSource> {
        protected readonly HashSet<IDumpSource> m_Sources = new HashSet<IDumpSource>(); 
        protected readonly HashSet<IDumpSystem> m_Systems = new HashSet<IDumpSystem>();

        private bool m_InitializeState = false;
        private bool m_FrozenState = false;

        /// <summary>
        /// Initializes all dump sources.
        /// </summary>
        public void Initialize() {
            if (!m_InitializeState) {
                m_InitializeState = true;
                foreach(var sys in m_Systems) {
                    sys.Initialize();
                }
            }
        }

        /// <summary>
        /// Shuts down all dump sources.
        /// </summary>
        public void Shutdown() {
            if (m_FrozenState) {
                throw new InvalidOperationException("Must call PostReport before Shutdown if PreReport was called");
            }

            if (m_InitializeState) {
                m_InitializeState = false;
                foreach(var sys in m_Systems) {
                    Shutdown();
                }
            }
        }

        internal void Freeze() {
            if (!m_InitializeState) {
                throw new InvalidOperationException("Must call Initialize before PreReport");
            }
            if (m_FrozenState) {
                throw new InvalidOperationException("Must call PostReport after any PreReport call before calling PreReport again");
            }
            
            m_FrozenState = true;
            foreach(var sys in m_Systems) {
                sys.Freeze();
            }
        }

        internal void GatherReports(IDumpWriter writer) {
            if (!m_InitializeState || !m_FrozenState) {
                throw new InvalidOperationException("Cannot gather reports if Initialize or PreReport have not been called");
            }

            foreach(var source in m_Sources) {
                source.Dump(writer);
            }
        }

        internal void Unfreeze() {
            if (!m_FrozenState) {
                throw new InvalidOperationException("Must call PostReport after PreReport");
            }
            
            m_FrozenState = false;
            foreach(var sys in m_Systems) {
                sys.Unfreeze();
            }
        }

        #region ICollection

        public int Count { get { return m_Sources.Count; } }

        public bool IsReadOnly  { get { return false; } }

        public void Add(IDumpSource item) {
            if (m_Sources.Add(item)) {
                IDumpSystem sys = item as IDumpSystem;
                if (sys != null && m_Systems.Add(sys)) {
                    if (m_InitializeState) {
                        sys.Initialize();
                    }
                    if (m_FrozenState) {
                        sys.Freeze();
                    }
                }
            }
        }

        public void Clear() {
            m_Sources.Clear();
            if (m_InitializeState) {
                foreach(var system in m_Systems) {
                    system.Shutdown();
                }
            }
            m_Systems.Clear();
        }

        public bool Contains(IDumpSource item) {
            return m_Sources.Contains(item);
        }

        public void CopyTo(IDumpSource[] array, int arrayIndex) {
            m_Sources.CopyTo(array, arrayIndex);
        }

        public IEnumerator<IDumpSource> GetEnumerator() {
            return m_Sources.GetEnumerator();
        }

        public bool Remove(IDumpSource item) {
            if (m_Sources.Remove(item)) {
                IDumpSystem sys = item as IDumpSystem;
                if (sys != null && m_Systems.Contains(sys)) {
                    if (m_FrozenState) {
                        sys.Freeze();
                    }
                    if (m_InitializeState) {
                        sys.Shutdown();
                    }
                    m_Systems.Remove(sys);
                }
                return true;
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion // ICollection
    }
}