using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public abstract class DBObjectCollection : ScriptableObject { }

    public abstract class DBObjectCollection<T> : DBObjectCollection
        where T : DBObject
    {
        #region Inspector

        [SerializeField] protected T[] m_Objects = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Constructed;
        protected Dictionary<StringHash32, T> m_IdMap;

        public int Count() { return m_Objects.Length; }
        public IReadOnlyList<T> Objects { get { return m_Objects; } }

        #region Unity Events

        protected virtual void OnEnable()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            #endif // UNITY_EDITOR
            
            EnsureCreated();
        }

        protected virtual void OnDisable()
        {
        }

        #endregion // Unity Events

        #region Id Resolution

        public bool HasId(StringHash32 inId)
        {
            return m_IdMap.ContainsKey(inId);
        }

        #endregion // Id Resolution

        #region Lookup

        public T Get(StringHash32 inId)
        {
            EnsureCreated();

            if (inId.IsEmpty)
                return NullValue();

            T obj;
            m_IdMap.TryGetValue(inId, out obj);
            Assert.NotNull(obj, "Could not find {0} with id '{1}'", typeof(T).Name, inId);
            return obj;
        }

        public T this[StringHash32 inId]
        {
            get { return Get(inId); }
        }

        public IEnumerable<T> Filter(Predicate<T> inPredicate)
        {
            if (inPredicate == null)
                throw new ArgumentNullException("inPredicate");
            
            for(int i = 0; i < m_Objects.Length; ++i)
            {
                T obj = m_Objects[i];
                if (inPredicate(obj))
                    yield return obj;
            }
        }

        protected virtual T NullValue() { return null; }

        #endregion // Lookup

        #region Internal

        public void Initialize()
        {
            EnsureCreated();
        }

        protected void EnsureCreated()
        {
            if (!m_Constructed)
            {
                m_Constructed = true;
                ConstructLookups();
            }
        }

        protected virtual void ConstructLookups()
        {
            PreLookupConstruct();

            for(int i = 0; i < m_Objects.Length; ++i)
            {
                T obj = m_Objects[i];
                ConstructLookupForItem(obj, i);
            }
        }

        protected virtual void PreLookupConstruct()
        {
            m_IdMap = new Dictionary<StringHash32, T>(m_Objects.Length);
        }

        protected virtual void ConstructLookupForItem(T inItem, int inIndex)
        {
            StringHash32 id = inItem.Id();
            Assert.False(m_IdMap.ContainsKey(id), "Duplicate {0} entry '{1}'", typeof(T).Name, id);
            m_IdMap.Add(id, inItem);
        }

        #endregion // Internal

        #region Editor

        #if UNITY_EDITOR

        protected void SortObjects(Comparison<T> inComparison)
        {
            Array.Sort(m_Objects, inComparison);
        }

        [UnityEditor.CustomEditor(typeof(DBObjectCollection<>))]
        protected class BaseInspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                bool bGUIEnabled = GUI.enabled;
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    GUI.enabled = false;
                }

                if (GUILayout.Button("Refresh"))
                {
                    DBObjectCollection<T> collection = (DBObjectCollection<T>) target;
                    collection.RefreshCollection();
                    UnityEditor.EditorUtility.SetDirty(collection);
                }

                GUI.enabled = bGUIEnabled;
            }
        }

        internal void RefreshCollection()
        {
            m_Objects = ValidationUtils.FindAllAssets<T>(IgnoreTemplates);
        }

        static private bool IgnoreTemplates(T inAsset)
        {
            return char.IsLetterOrDigit(inAsset.name[0]);
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}