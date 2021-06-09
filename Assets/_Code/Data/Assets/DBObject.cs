using System;
using BeauUtil;
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
    }
}