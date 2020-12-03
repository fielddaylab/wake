using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public abstract class DBObjectCollection<T> : ScriptableObject
        where T : DBObject
    {
        #region Inspector

        [SerializeField] private T[] m_Objects = null;

        #endregion // Inspector

        private bool m_Constructed;
        private StringHash32[] m_Ids;
        protected Dictionary<StringHash32, T> m_IdMap;
        protected Dictionary<StringHash32, StringHash32> m_ScriptNameMap;

        public int Count() { return m_Objects.Length; }

        public IReadOnlyList<T> Objects { get { return m_Objects; } }
        public IReadOnlyList<StringHash32> Ids { get { EnsureCreated(); return m_Ids; } }

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

        public StringHash32 ScriptNameToId(StringHash32 inScriptName)
        {
            EnsureCreated();

            StringHash32 id;
            m_ScriptNameMap.TryGetValue(inScriptName, out id);
            return id;
        }

        #endregion // Id Resolution

        #region Lookup

        public T Get(StringHash32 inId)
        {
            T obj;
            m_IdMap.TryGetValue(inId, out obj);
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

        #endregion // Lookup

        #region Internal

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
            m_Ids = new StringHash32[m_Objects.Length];
            m_IdMap = new Dictionary<StringHash32, T>(m_Objects.Length);
            m_ScriptNameMap = new Dictionary<StringHash32, StringHash32>(m_Objects.Length);
        }

        protected virtual void ConstructLookupForItem(T inItem, int inIndex)
        {
            StringHash32 id = inItem.Id();
            StringHash32 scriptName = inItem.ScriptName();

            m_Ids[inIndex] = id;
            m_IdMap.Add(id, inItem);
            if (!scriptName.IsEmpty)
                m_ScriptNameMap.Add(scriptName, id);
        }

        #endregion // Internal

        #region Editor

        #if UNITY_EDITOR

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
                    collection.m_Objects = ValidationUtils.FindAllAssets<T>();
                    UnityEditor.EditorUtility.SetDirty(collection);
                }

                GUI.enabled = bGUIEnabled;
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}