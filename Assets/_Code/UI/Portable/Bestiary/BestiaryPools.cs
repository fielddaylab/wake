using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable {
    public sealed class BestiaryPools : MonoBehaviour {

        #region Inspector

        [SerializeField] private PortableStationHeader.Pool m_HeaderPool = null;
        [SerializeField] private PortableBestiaryToggle.Pool m_EntryPool = null;

        #endregion // Inspector

        public void Clear() {
            m_HeaderPool.Reset();
            m_EntryPool.Reset();
        }

        public PortableStationHeader AllocHeader(Transform root) {
            return m_HeaderPool.Alloc(root);
        }

        public PortableBestiaryToggle AllocEntry(Transform root) {
            return m_EntryPool.Alloc(root);
        }

        public void PrewarmEntries(int count) {
            m_EntryPool.Prewarm(count);
        }

        public IEnumerable<PortableBestiaryToggle> AllEntries() {
            return m_EntryPool.ActiveObjects;
        }
    }
}