using BeauUtil;
using UnityEngine;
using ProtoAqua.Scripting;

namespace ProtoAqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        #region Access

        /// <summary>
        /// Attempts to get a script node, starting from the scope of the given node.
        /// </summary>
        public bool TryGetScriptNode(ScriptNode inScope, StringHash32 inId, out ScriptNode outNode)
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
        public bool TryGetEntrypoint(StringHash32 inId, out ScriptNode outNode)
        {
            return m_LoadedEntrypoints.TryGetValue(inId, out outNode);
        }

        /// <summary>
        /// Returns if an entrypoint exists for the given id.
        /// </summary>
        public bool HasEntrypoint(StringHash32 inId)
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
                StringHash32 id = entrypoint.Id();
                ScriptNode existingEntrypoint;
                if (m_LoadedEntrypoints.TryGetValue(id, out existingEntrypoint))
                {
                    Debug.LogWarningFormat("[ScriptingService] Duplicate script node entrypoints '{0}' in package '{1}' and '{2}'", id.ToDebugString(), existingEntrypoint.Package().Name(), entrypoint.Package().Name());
                    continue;
                }

                m_LoadedEntrypoints.Add(id, entrypoint);
                ++entrypointCount;
            }

            int responseCount = 0;
            foreach(var response in inPackage.Responses())
            {
                StringHash32 triggerId = response.TriggerData.TriggerId;
                TriggerResponseSet responseSet;
                if (!m_LoadedResponses.TryGetValue(triggerId, out responseSet))
                {
                    responseSet = new TriggerResponseSet();
                    m_LoadedResponses.Add(triggerId, responseSet);
                }

                responseSet.AddNode(response);
                ++responseCount;
            }

            Debug.LogFormat("[ScriptingService] Loaded package '{0}' with '{1}' entrypoints and '{2}' responses", inPackage.Name(), entrypointCount, responseCount);
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
                StringHash32 id = entrypoint.Id();
                ScriptNode existingEntrypoint;
                if (m_LoadedEntrypoints.TryGetValue(id, out existingEntrypoint) && existingEntrypoint == entrypoint)
                {
                    m_LoadedEntrypoints.Remove(id);
                }
            }

            foreach(var response in inPackage.Responses())
            {
                StringHash32 triggerId = response.TriggerData.TriggerId;
                TriggerResponseSet responseSet;
                if (m_LoadedResponses.TryGetValue(triggerId, out responseSet))
                {
                    responseSet.RemoveNode(response);
                }
            }

            Debug.LogFormat("[ScriptingService] Unloaded package '{0}'", inPackage.Name());
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