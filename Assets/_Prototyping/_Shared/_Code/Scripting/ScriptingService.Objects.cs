using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        #region Access

        /// <summary>
        /// Attempts to get a script node, starting from the scope of the given node.
        /// </summary>
        public bool TryGetScriptNode(ScriptNode inScope, string inId, out ScriptNode outNode)
        {
            if (inScope.Package().TryGetNode(inId, out outNode))
            {
                return true;
            }

            return TryGetEntrypoint(inId, out outNode);
        }

        /// <summary>
        /// Attempts to get a publically-exposed script node.
        /// </summary>
        public bool TryGetEntrypoint(string inId, out ScriptNode outNode)
        {
            return m_LoadedEntrypoints.TryGetValue(inId, out outNode);
        }

        /// <summary>
        /// Returns if an entrypoint exists for the given id.
        /// </summary>
        public bool HasEntrypoint(string inId)
        {
            return m_LoadedEntrypoints.ContainsKey(inId);
        }

        #endregion // Access

        #region Loading

        /// <summary>
        /// Loads a script package.
        /// </summary>
        public void Load(ScriptNodePackage inPackage)
        {
            if (!m_LoadedPackages.Add(inPackage))
                return;

            int entrypointCount = 0;
            foreach(var entrypoint in inPackage.Entrypoints())
            {
                string id = entrypoint.Id();
                ScriptNode existingEntrypoint;
                if (m_LoadedEntrypoints.TryGetValue(id, out existingEntrypoint))
                {
                    Debug.LogWarningFormat("[ScriptingService] Duplicate script node entrypoints '{0}' in package '{1}' and '{2}'", id, existingEntrypoint.Package().Name(), entrypoint.Package().Name());
                    continue;
                }

                m_LoadedEntrypoints.Add(id, entrypoint);
                ++entrypointCount;
            }

            Debug.LogFormat("[ScriptingService] Loaded package '{0}' with '{1}' non-errored entrypoints", inPackage.Name(), entrypointCount);
        }

        /// <summary>
        /// Unloads a script package.
        /// </summary>
        public void Unload(ScriptNodePackage inPackage)
        {
            if (!m_LoadedPackages.Remove(inPackage))
                return;

            foreach(var entrypoint in inPackage.Entrypoints())
            {
                string id = entrypoint.Id();
                ScriptNode existingEntrypoint;
                if (m_LoadedEntrypoints.TryGetValue(id, out existingEntrypoint) && existingEntrypoint == entrypoint)
                {
                    m_LoadedEntrypoints.Remove(id);
                }
            }
        }

        #endregion // Loading
    
        #region Objects

        // private class ScriptObjectList
        // {
        //     private readonly List<ScriptObject> m_Objects = new List<ScriptObject>(16);
        // }

        // public bool TryRegister(ScriptObject inObject)
        // {
        //     if (!m_ScriptObjects.Contains(inObject))
        //         return false;

        //     m_ScriptObjects.Add()
        // }

        #endregion // Objects
    }
}