using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using ProtoAqua;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoCP
{
    [CreateAssetMenu(menuName = "Prototype/CP/Style")]
    public class CPStyle : ScriptableObject
    {
        #region Types

        [Serializable] protected class ControlPool : SerializablePool<CPControl> { }

        private class ControlPoolSet
        {
            private readonly Dictionary<string, ControlPool> m_Variants;
            private ControlPool m_DefaultPool;

            public ControlPoolSet()
            {
                m_Variants = new Dictionary<string, ControlPool>(3, StringComparer.Ordinal);
            }

            public bool TryGetPool(string inVariantId, out ControlPool outPool)
            {
                if (string.IsNullOrEmpty(inVariantId))
                {
                    outPool = m_DefaultPool;
                    return outPool != null;
                }

                return m_Variants.TryGetValue(inVariantId, out outPool);
            }

            public void AddPool(CPControl inTemplate, Transform inRoot, int inPrewarm)
            {
                bool bDefault = m_DefaultPool == null;
                string variantId = inTemplate.VariantId();

                if (string.IsNullOrEmpty(variantId))
                {
                    bDefault = true;
                    variantId = inTemplate.name;
                }

                if (m_Variants.ContainsKey(variantId))
                {
                    Debug.LogErrorFormat("[CPStyle] Multiple controls with variant id '{0}' defined for type '{1}'", variantId, inTemplate.Type());
                    return;
                }

                ControlPool pool = new ControlPool();
                pool.Name = variantId;
                pool.Prefab = inTemplate;
                pool.ConfigureTransforms(inRoot, null, true);
                pool.ConfigureCapacity(inPrewarm * 4, inPrewarm, true);

                m_Variants.Add(variantId, pool);

                if (bDefault)
                {
                    m_DefaultPool = pool;
                }
            }
        
            public void Shutdown()
            {
                m_DefaultPool?.Destroy();
                m_DefaultPool = null;

                foreach(var pool in m_Variants.Values)
                {
                    pool.Destroy();
                }

                m_Variants.Clear();
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField, EditModeOnly] private CPStyle m_InheritFrom = null;
        
        [Header("Controls")]
        [SerializeField, EditModeOnly] private CPControl[] m_ControlTemplates = null;
        [SerializeField, EditModeOnly] private int m_Prewarm = 4;
        
        [Header("Visuals")]
        [SerializeField] private float m_IndentSize = 32;

        #endregion // Inspector

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private Transform m_PoolRoot = null;
        [NonSerialized] private Dictionary<FourCC, ControlPoolSet> m_PoolSets;

        [NonSerialized] private readonly Action m_ShutdownDelegate;

        private CPStyle()
        {
            m_ShutdownDelegate = Shutdown;
        }

        #region Alloc

        public CPControl Alloc(FourCC inControlType, Transform inTarget)
        {
            return Alloc(inControlType, null, inTarget);
        }

        public CPControl Alloc(FourCC inControlType, string inVariantId, Transform inTarget)
        {
            Initialize();

            ControlPoolSet poolSet = null;
            ControlPool pool = null;

            if (m_PoolSets.TryGetValue(inControlType, out poolSet))
            {
                poolSet.TryGetPool(inVariantId, out pool);
            }

            if (pool == null)
            {
                if (m_InheritFrom != null)
                {
                    return m_InheritFrom.Alloc(inControlType, inTarget);
                }
                else
                {
                    Debug.LogErrorFormat("[CPStyle] No control pool available for control type '{0}'", inControlType);
                    return null;
                }
            }

            CPControl control = pool.Alloc(inTarget);
            control.SetStyle(this);
            return control;
        }

        public T Alloc<T>(FourCC inControlType, Transform inTarget) where T : CPControl
        {
            return Alloc<T>(inControlType, null, inTarget);
        }

        public T Alloc<T>(FourCC inControlType, string inVariantId, Transform inTarget) where T : CPControl
        {
            Initialize();

            ControlPoolSet poolSet = null;
            ControlPool pool = null;
            T control;

            if (m_PoolSets.TryGetValue(inControlType, out poolSet))
            {
                poolSet.TryGetPool(inVariantId, out pool);
            }

            if (pool == null || !(pool.Prefab is T))
            {
                if (m_InheritFrom != null)
                {
                    control = m_InheritFrom.Alloc<T>(inControlType, inTarget);
                }
                else
                {
                    Debug.LogErrorFormat("[CPStyle] No control pool available for control type '{0}' and c# type '{1}'", inControlType, typeof(T).Name);
                    return null;
                }
            }
            else
            {
                control = (T) pool.Alloc(inTarget);
            }

            control.SetStyle(this);
            return control;
        }

        #endregion // Alloc

        public float IndentSize() { return m_IndentSize; }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void Initialize()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            #endif // UNITY_EDITOR

            if (m_Initialized)
                return;

            Debug.LogFormat("[CPStyle] Loading style '{0}'...", name);

            GameObject poolRootGO = new GameObject(string.Format("CPStyle Storage ({0})", name));
            poolRootGO.hideFlags = HideFlags.DontSave;
            poolRootGO.SetActive(false);

            DontDestroyOnLoad(poolRootGO);
            m_PoolRoot = poolRootGO.transform;

            m_PoolSets = new Dictionary<FourCC, ControlPoolSet>(m_ControlTemplates.Length);

            foreach(var template in m_ControlTemplates)
            {
                FourCC type = template.Type();
                ControlPoolSet set;
                if (!m_PoolSets.TryGetValue(type, out set))
                {
                    set = new ControlPoolSet();
                    m_PoolSets.Add(type, set);
                }

                set.AddPool(template, m_PoolRoot, m_Prewarm);
            }

            Application.quitting += m_ShutdownDelegate;
            m_Initialized = true;

            Debug.LogFormat("[CPStyle] ...finished loading style '{0}'", name);
        }

        private void Shutdown()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            #endif // UNITY_EDITOR

            if (!m_Initialized)
                return;

            Application.quitting -= m_ShutdownDelegate;

            Debug.LogFormat("[CPStyle] Unloading style '{0}'...", name);

            foreach(var poolCollection in m_PoolSets.Values)
            {
                poolCollection.Shutdown();
            }

            m_PoolSets.Clear();
            UnityHelper.SafeDestroyGO(ref m_PoolRoot);

            m_Initialized = false;

            Debug.LogFormat("[CPStyle] ...finished unloading style '{0}'...", name);
        }
    
        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (m_InheritFrom == this)
            {
                m_InheritFrom = null;
                Debug.LogErrorFormat("[CPStyle] Cannot inherit from self");
            }
            ValidationUtils.EnsureUnique(ref m_ControlTemplates);
        }

        #endif // UNITY_EDITOR
    }
}