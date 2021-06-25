using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public abstract class DBObject : ScriptableObject
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_ScriptName = StringHash32.Null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_HashedId;

        public StringHash32 Id() { return m_HashedId.IsEmpty ? (m_HashedId = name) : m_HashedId; }
        public StringHash32 ScriptName() { return m_ScriptName; }

        #if UNITY_EDITOR

        static internal void RefreshCollection<T, U>()
            where T : DBObject
            where U : DBObjectCollection<T>
        {
            foreach(var v in ValidationUtils.FindAllAssets<U>())
            {
                Log.Msg("[DBObject] Refreshing {0} database", typeof(T).Name);

                UnityEditor.Undo.RecordObject(v, "Refreshing db");
                v.RefreshCollection();
                UnityEditor.EditorUtility.SetDirty(v);
            }
        }

        #endif // UNITY_EDITOR
    }
}