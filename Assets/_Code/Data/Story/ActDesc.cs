using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.IO;
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
            if (m_ScriptingRef == null)
            {
                m_ScriptingRef = new ReloadableRef<LeafAsset>(m_Scripting);
            }
            return m_ScriptingRef;
            #else
            return m_Scripting;
            #endif // UNITY_EDITOR
        }

        #if UNITY_EDITOR

        [NonSerialized] private ReloadableRef<LeafAsset> m_ScriptingRef = null;

        internal void EditorInit()
        {
            m_ScriptingRef = new ReloadableRef<LeafAsset>(m_Scripting);
        }

        #endif // UNITY_EDITOR
    }
}