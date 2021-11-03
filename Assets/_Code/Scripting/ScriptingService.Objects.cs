using BeauUtil;
using UnityEngine;
using Aqua.Scripting;
using System.Collections.Generic;
using BeauPools;
using Leaf;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using Aqua.Debugging;
using System.Collections;
using BeauRoutine;

namespace Aqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        #region Access

        /// <summary>
        /// Attempts to get a script node, starting from the scope of the given node.
        /// </summary>
        internal bool TryGetScriptNode(ScriptNode inScope, StringHash32 inId, out ScriptNode outNode)
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
        internal bool TryGetEntrypoint(StringHash32 inId, out ScriptNode outNode)
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

        #region Packages

        public void LoadScript(LeafAsset inAsset)
        {
            if (m_LoadedPackageSourcesAssets.ContainsKey(inAsset) || m_CurrentPackageBeingLoaded == inAsset || m_PackageLoadQueue.Contains(inAsset))
                return;

            m_PackageLoadQueue.PushBack(inAsset);
            if (!m_PackageLoadWorker)
            {
                m_PackageLoadWorker = Routine.Start(this, PackageLoadLoop());
            }
        }

        public void UnloadScript(LeafAsset inAsset)
        {
            if (CancelPackageLoad(inAsset))
                return;

            ScriptNodePackage package;
            if (m_LoadedPackageSourcesAssets.TryGetValue(inAsset, out package))
            {
                package.UnbindAsset();
                RemovePackage(package);
                m_LoadedPackageSourcesAssets.Remove(inAsset);
            }
        }

        internal void AddPackage(ScriptNodePackage inPackage)
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
                    Log.Warn("[ScriptingService] Duplicate script node entrypoints '{0}' in package '{1}' and '{2}'", id, existingEntrypoint.Package().Name(), entrypoint.Package().Name());
                    continue;
                }

                m_LoadedEntrypoints.Add(id, entrypoint);
                ++entrypointCount;
            }

            int responseCount = 0;
            foreach(var response in inPackage.Responses())
            {
                StringHash32 triggerId = response.TriggerOrFunctionId();
                TriggerResponseSet responseSet;
                if (!m_LoadedResponses.TryGetValue(triggerId, out responseSet))
                {
                    responseSet = new TriggerResponseSet();
                    m_LoadedResponses.Add(triggerId, responseSet);
                }

                responseSet.AddNode(response);
                ++responseCount;
            }

            int functionCount = 0;
            foreach(var function in inPackage.Functions())
            {
                StringHash32 functionId = function.TriggerOrFunctionId();
                FunctionSet funcSet;
                if (!m_LoadedFunctions.TryGetValue(functionId, out funcSet))
                {
                    funcSet = new FunctionSet();
                    m_LoadedFunctions.Add(functionId, funcSet);
                }

                funcSet.AddNode(function);
                ++responseCount;
            }

            DebugService.Log(LogMask.Loading | LogMask.Scripting, "[ScriptingService] Added package '{0}' with {1} entrypoints, {2} responses, {3} functions", inPackage.Name(), entrypointCount, responseCount, functionCount);
        }

        internal void RemovePackage(ScriptNodePackage inPackage)
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
                StringHash32 triggerId = response.TriggerOrFunctionId();
                TriggerResponseSet responseSet;
                if (m_LoadedResponses.TryGetValue(triggerId, out responseSet))
                {
                    responseSet.RemoveNode(response);
                }
            }

            foreach(var function in inPackage.Functions())
            {
                StringHash32 functionId = function.TriggerOrFunctionId();
                FunctionSet funcSet;
                if (m_LoadedFunctions.TryGetValue(functionId, out funcSet))
                {
                    funcSet.RemoveNode(function);
                }
            }

            DebugService.Log(LogMask.Loading | LogMask.Scripting, "[ScriptingService] Removed package '{0}'", inPackage.Name());
        }

        private bool CancelPackageLoad(LeafAsset inAsset)
        {
            if (m_CurrentPackageBeingLoaded == inAsset)
            {
                m_CurrentPackageBeingLoaded = null;
                m_CurrentPackageLoadHandle.Cancel();
                return true;
            }

            return false;
        }

        private IEnumerator PackageLoadLoop()
        {
            LeafAsset assetToLoad;
            IEnumerator loader;
            ScriptNodePackage package;
            while(m_PackageLoadQueue.TryPopFront(out assetToLoad))
            {
                m_CurrentPackageBeingLoaded = assetToLoad;
                package = LeafAsset.CompileAsync(assetToLoad, ScriptNodePackage.Generator.Instance, out loader);
                m_CurrentPackageLoadHandle = Async.Schedule(loader, AsyncFlags.HighPriority);
                using(Profiling.Time(string.Format("loading script '{0}'", assetToLoad.name))) {
                    yield return m_CurrentPackageLoadHandle;
                }

                if (m_CurrentPackageBeingLoaded != assetToLoad) {
                    package.Clear();
                    continue;
                }

                package.BindAsset(assetToLoad);
                AddPackage(package);
                m_LoadedPackageSourcesAssets.Add(assetToLoad, package);
                // yield return null;
            }

            m_CurrentPackageBeingLoaded = null;
            m_CurrentPackageLoadHandle = default;
        }

        #endregion // Packages
    
        #region Objects

        public bool TryRegisterObject(ScriptObject inObject)
        {
            if (m_ScriptObjects.Contains(inObject))
                return false;

            m_ScriptObjects.Add(inObject);
            m_ScriptObjectListDirty = true;
            return true;
        }

        public bool TryDeregisterObject(ScriptObject inObject)
        {
            if (m_ScriptObjects.FastRemove(inObject))
            {
                m_ScriptObjectListDirty = true;
                return true;
            }

            return false;
        }

        public bool TryGetScriptObjectById(StringHash32 inId, out ScriptObject outObject)
        {
            UndirtyScriptObjectList();
            return m_ScriptObjects.TryBinarySearch(inId, out outObject);
        }

        public IEnumerable<ScriptObject> GetScriptObjects(StringSlice inIdentifier)
        {
            ScriptObject scObj;
            if (TryGetScriptObjectById(inIdentifier.Substring(1), out scObj))
            {
                yield return scObj;
            }
        }

        private void UndirtyScriptObjectList()
        {
            if (m_ScriptObjectListDirty)
            {
                m_ScriptObjects.SortByKey<StringHash32, ScriptObject, ScriptObject>();
                m_ScriptObjectListDirty = false;
            }
        }

        #endregion // Objects
    
        #region Actors

        #endregion // Actors
    }
}