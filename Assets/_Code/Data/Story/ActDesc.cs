using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Act Description", fileName = "NewActDesc")]
    public class ActDesc : DBObject
    {
        #region Inspector

        [Header("Assets")]
        [SerializeField] private LeafAsset m_Scripting = null;

        #endregion // Inspector

        public LeafAsset Scripting()
        {
            #if UNITY_EDITOR
            return m_ScriptingRef;
            #else
            return m_Scripting;
            #endif // UNITY_EDITOR
        }

        #if UNITY_EDITOR

        [NonSerialized] private ReloadableAssetRef<LeafAsset> m_ScriptingRef = null;

        internal void EditorInit()
        {
            m_ScriptingRef = new ReloadableAssetRef<LeafAsset>(m_Scripting);
        }

        #endif // UNITY_EDITOR
    }
}