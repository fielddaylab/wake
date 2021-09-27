#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using Aqua.Debugging;
using BeauUtil.Services;
using Leaf.Runtime;

namespace Aqua
{
    [ServiceDependency(typeof(UIMgr))]
    public partial class StateMgr : ServiceBehaviour, IDebuggable
    {
        #region Inspector

        [SerializeField, Required] private GameObject m_InitialPreloadRoot = null;

        #endregion // Inspector

        private Routine m_SceneLoadRoutine;
        [NonSerialized] private StringHash32 m_EntranceId;
        [NonSerialized] private bool m_SceneLock;

        private VariantTable m_TempSceneTable;

        private RingBuffer<SceneBinding> m_SceneHistory = new RingBuffer<SceneBinding>(8, RingBufferMode.Overwrite);
        private Dictionary<Type, SharedManager> m_SharedManagers;

        public StringHash32 LastEntranceId { get { return m_EntranceId; } }

        #region Scene Loading

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(string inSceneName, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Log.Error("[StateMgr] Scene load already in progress");
                return null;
            }

            SceneBinding scene = SceneHelper.FindSceneByName(inSceneName, SceneCategories.Build);
            if (!scene.IsValid())
            {
                Log.Error("[StateMgr] No scene found with name matching '{0}'", inSceneName);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(scene, inEntrance, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to another scene from a given map id.
        /// </summary>
        public IEnumerator LoadSceneFromMap(StringHash32 inMapId, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Log.Error("[StateMgr] Scene load already in progress");
                return null;
            }

            MapDesc map = Assets.Map(inMapId);
            if (!map)
            {
                Log.Error("[StateMgr] No map found with id '{0}'", inMapId);
                return null;
            }

            SceneBinding scene = SceneHelper.FindSceneByName(map.SceneName(), SceneCategories.Build);
            if (!scene.IsValid())
            {
                Log.Error("[StateMgr] No scene found with name matching '{0}' on map '{1}'", map.SceneName(), inMapId);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(scene, inEntrance, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(SceneBinding inScene, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Log.Error("[StateMgr] Scene load already in progress");
                return null;
            }

            if (!inScene.IsValid())
            {
                Log.Error("[StateMgr] Provided scene '{0}' is not valid", inScene);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(inScene, inEntrance, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(StringHash32 inSceneId, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Log.Error("[StateMgr] Scene load already in progress");
                return null;
            }

            SceneBinding scene = SceneHelper.FindSceneById(inSceneId, SceneCategories.Build);
            if (!scene.IsValid())
            {
                Log.Error("[StateMgr] No scene found with id '{0}'", inSceneId);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(scene, inEntrance, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to the previously loaded scene.
        /// </summary>
        public IEnumerator LoadPreviousScene(string inDefault = null, StringHash32 inEntrance = default(StringHash32), object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Log.Error("[StateMgr] Scene load already in progress");
                return null;
            }

            if (m_SceneHistory.Count < 2)
            {
                if (!string.IsNullOrEmpty(inDefault))
                {
                    return LoadScene(inDefault, inEntrance, inContext, inFlags);
                }

                Log.Error("[StateMgr] No previous scene in scene history");
                return null;
            }

            // pop the current scene
            m_SceneHistory.PopBack();

            // get the previous one
            SceneBinding prevScene = m_SceneHistory.PeekBack();

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(prevScene, inEntrance, inContext, inFlags | SceneLoadFlags.DoNotModifyHistory)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Reloads the current scene.
        /// </summary>
        public IEnumerator ReloadCurrentScene(object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            return LoadScene(SceneHelper.ActiveScene(), m_EntranceId, inContext, inFlags);
        }

        /// <summary>
        /// Returns if loading into another scene.
        /// </summary>
        public bool IsLoadingScene()
        {
            return m_SceneLock;
        }

        /// <summary>
        /// Returns the previous scene.
        /// </summary>
        public SceneBinding PreviousScene()
        {
            if (m_SceneHistory.Count > 1)
                return m_SceneHistory[m_SceneHistory.Count - 2];
            return default(SceneBinding);
        }

        private IEnumerator InitialSceneLoad()
        {
            yield return WaitForServiceLoading();
            
            Services.Input.PauseAll();
            yield return WaitForPreload(m_InitialPreloadRoot, null);

            foreach(var obj in m_InitialPreloadRoot.GetComponentsInChildren<ISceneLoadHandler>(true))
                obj.OnSceneLoad(m_InitialPreloadRoot.scene, null);

            DebugService.Log(LogMask.Loading, "[StateMgr] Initial load of preload scene '{0}' finished", m_InitialPreloadRoot.scene.path);

            SceneBinding active = SceneManager.GetActiveScene();
            BindScene(active);
            m_SceneHistory.PushBack(active);

            #if DEVELOPMENT
            // if we started from another scene than the boot or title scene
            if (active.BuildIndex >= GameConsts.GameSceneIndexStart)
                Services.Data.UseDebugProfile();
            #endif // DEVELOPMENT

            #if UNITY_EDITOR
            yield return WaitForOptimize(active, null);
            #else
            yield return LoadConditionalSubscenes(active, null);
            #endif // UNITY_EDITOR

            yield return WaitForPreload(active, null);
            yield return WaitForServiceLoading();
            yield return WaitForCleanup();

            m_SceneLock = false;
            Services.UI.HideLoadingScreen();

            DebugService.Log(LogMask.Loading, "[StateMgr] Initial load of '{0}' finished", active.Path);

            active.BroadcastLoaded();
            Services.Input.ResumeAll();
            Services.Physics.Enabled = true;

            Services.Events.Dispatch(GameEvents.SceneLoaded);
            Services.Script.TriggerResponse(GameTriggers.SceneStart);
        }

        private IEnumerator SceneSwap(SceneBinding inNextScene, StringHash32 inEntrance, object inContext, SceneLoadFlags inFlags)
        {
            Services.Input.PauseAll();
            Services.Script.KillLowPriorityThreads();
            Services.Physics.Enabled = false;
            BootParams.ClearStartFlag();

            bool bShowLoading = (inFlags & SceneLoadFlags.NoLoadingScreen) == 0;
            bool bShowCutscene = (inFlags & SceneLoadFlags.Cutscene) != 0;
            if (bShowCutscene)
            {
                Services.UI.ShowLetterbox();
            }

            if ((inFlags & SceneLoadFlags.DoNotDispatchPreUnload) == 0)
                Services.Events.Dispatch(GameEvents.SceneWillUnload);

            if (bShowLoading)
            {
                yield return Services.UI.ShowLoadingScreen();
            }

            #if DEVELOPMENT
            if (inNextScene.BuildIndex >= GameConsts.GameSceneIndexStart && !Services.Data.IsProfileLoaded())
                Services.Data.UseDebugProfile();
            #endif // DEVELOPMENT

            SceneBinding active = SceneHelper.ActiveScene();
            m_EntranceId = inEntrance;

            // unloading instant
            DebugService.Log(LogMask.Loading, "[StateMgr] Unloading scene '{0}'", active.Path);

            active.BroadcastUnload(inContext);
            
            Services.Deregister(active);

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(inNextScene.Path, LoadSceneMode.Single);
            loadOp.allowSceneActivation = false;

            DebugService.Log(LogMask.Loading, "[StateMgr] Loading scene '{0}' with entrance '{1}'", inNextScene.Path, m_EntranceId);

            while(loadOp.progress < 0.9f)
                yield return null;

            loadOp.allowSceneActivation = true;

            while(!loadOp.isDone)
                yield return null;

            BindScene(inNextScene);
            yield return WaitForServiceLoading();

            if ((inFlags & SceneLoadFlags.DoNotModifyHistory) == 0)
            {
                m_SceneHistory.PushBack(inNextScene);
            }

            #if UNITY_EDITOR
            yield return WaitForOptimize(inNextScene, inContext);
            #else
            yield return LoadConditionalSubscenes(inNextScene, inContext);
            #endif // UNITY_EDITOR

            yield return WaitForPreload(inNextScene, inContext);
            yield return WaitForServiceLoading();
            yield return WaitForCleanup();

            m_SceneLock = false;

            if (bShowLoading)
            {
                Services.UI.HideLoadingScreen();
            }
            if (bShowCutscene)
            {
                Services.UI.HideLetterbox();
            }

            DebugService.Log(LogMask.Loading, "[StateMgr] Finished loading scene '{0}'", inNextScene.Path);
            inNextScene.BroadcastLoaded(inContext);
            Services.Input.ResumeAll();
            Services.Physics.Enabled = true;
            m_SceneLock = false;

            Services.Events.Dispatch(GameEvents.SceneLoaded);
            Services.Script.TriggerResponse(GameTriggers.SceneStart);
        }

        private IEnumerator WaitForServiceLoading()
        {
            while(BuildInfo.IsLoading())
                yield return null;

            foreach(var service in Services.AllLoadable())
            {
                if (ReferenceEquals(this, service))
                    continue;
                
                while(service.IsLoading())
                    yield return null;
            }
        }

        private IEnumerator WaitForPreload(SceneBinding inScene, object inContext)
        {
            Services.Events.Dispatch(GameEvents.ScenePreloading);
            
            using(PooledList<IScenePreloader> allPreloaders = PooledList<IScenePreloader>.Create())
            {
                inScene.Scene.GetAllComponents<IScenePreloader>(true, allPreloaders);
                if (allPreloaders.Count > 0)
                {
                    DebugService.Log(LogMask.Loading, "[StateMgr] Executing preload steps for scene '{0}'", inScene.Path);
                    return Routine.ForEachParallel(allPreloaders.ToArray(), (p) => p.OnPreloadScene(inScene, inContext));
                }
            }

            return null;
        }

        private IEnumerator WaitForPreload(GameObject inRoot, object inContext)
        {
            using(PooledList<IScenePreloader> allPreloaders = PooledList<IScenePreloader>.Create())
            {
                inRoot.GetComponentsInChildren<IScenePreloader>(true, allPreloaders);
                if (allPreloaders.Count > 0)
                {
                    DebugService.Log(LogMask.Loading, "[StateMgr] Executing preload steps for gameObject '{0}'", inRoot.FullPath(true));
                    return Routine.ForEachParallel(allPreloaders.ToArray(), (p) => p.OnPreloadScene(inRoot.scene, inContext));
                }
            }

            return null;
        }

        private IEnumerator WaitForCleanup()
        {
            using(Profiling.Time("gc collect"))
            {
                GC.Collect();
            }
            using(Profiling.Time("unload unused assets"))
            {
                yield return Resources.UnloadUnusedAssets();
            }
        }

        private IEnumerator LoadConditionalSubscenes(SceneBinding inBinding, object inContext)
        {
            using(PooledList<ISceneSubsceneSelector> selectors = PooledList<ISceneSubsceneSelector>.Create())
            using(PooledList<string> subScenes = PooledList<string>.Create())
            {
                inBinding.Scene.GetAllComponents<ISceneSubsceneSelector>(false, selectors);
                if (selectors.Count > 0)
                {
                    foreach(var selector in selectors)
                    {
                        foreach(var scenePath in selector.GetAdditionalScenesNames(inBinding, inContext))
                        {
                            if (!string.IsNullOrEmpty(scenePath))
                                subScenes.Add(scenePath);
                        }
                    }
                }

                if (subScenes.Count > 0)
                {
                    DebugService.Log(LogMask.Loading, "[StateMgr] Loading {0} conditional subscenes...", subScenes.Count);
                    using(Profiling.Time("load conditional subscenes"))
                    {
                        foreach(var subscenePath in subScenes)
                        {
                            yield return LoadSubSceneFromName(subscenePath, inBinding);
                        }
                    }
                }
            }
        }

        #if UNITY_EDITOR
        
        private IEnumerator WaitForOptimize(SceneBinding inBinding, object inContext)
        {
            using(PooledList<SubScene> subScenes = PooledList<SubScene>.Create())
            {
                inBinding.Scene.GetAllComponents<SubScene>(false, subScenes);
                if (subScenes.Count > 0)
                {
                    DebugService.Log(LogMask.Loading, "[StateMgr] Loading {0} subscenes...", subScenes.Count);
                    using(Profiling.Time("load subscenes"))
                    {
                        foreach(var subscene in subScenes)
                        {
                            yield return LoadSubScene(subscene, inBinding);
                        }
                    }
                }
            }

            yield return LoadConditionalSubscenes(inBinding, inContext);

            using(PooledList<FlattenHierarchy> allFlatten = PooledList<FlattenHierarchy>.Create())
            {
                inBinding.Scene.GetAllComponents<FlattenHierarchy>(false, allFlatten);
                if (allFlatten.Count > 0)
                {
                    DebugService.Log(LogMask.Loading, "[StateMgr] Flattening {0} transform hierarchies...", allFlatten.Count);
                    using(Profiling.Time("flatten scene hierarchy"))
                    {
                        yield return Routine.Inline(Routine.ForEachAmortize(allFlatten, (f) => f.Flatten(), 5));
                    }
                }
            }

            using(PooledList<ISceneOptimizable> allOptimizable = PooledList<ISceneOptimizable>.Create())
            {
                inBinding.Scene.GetAllComponents<ISceneOptimizable>(true, allOptimizable);
                if (allOptimizable.Count > 0)
                {
                    DebugService.Log(LogMask.Loading, "[StateMgr] Optimizing {0} objects...", allOptimizable.Count);
                    using(Profiling.Time("optimize objects"))
                    {
                        yield return Routine.Inline(Routine.ForEachAmortize(allOptimizable, (f) => {
                            Debug.LogFormat("[StateMgr] ...optimizing {0}", f.ToString());
                            f.Optimize(); 
                        }, 5));
                    }
                }
            }
        }

        static private IEnumerator LoadSubScene(SubScene inSubScene, SceneBinding inActiveScene)
        {
            string path = inSubScene.Scene.Path;
            Destroy(inSubScene.gameObject);

            #if UNITY_EDITOR
            var editorScene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(path);
            if (!editorScene.isLoaded)
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            }
            #else
            yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            #endif // UNITY_EDITOR
            
            SceneBinding unityScene = SceneHelper.FindSceneByPath(path, SceneCategories.Loaded);
            GameObject[] roots = unityScene.Scene.GetRootGameObjects();
            foreach(var root in roots)
            {
                SceneManager.MoveGameObjectToScene(root, inActiveScene);
            }
            yield return SceneManager.UnloadSceneAsync(unityScene);
        }

        #endif // UNITY_EDITOR

        static private IEnumerator LoadSubSceneFromName(string inSceneName, SceneBinding inActiveScene)
        {
            #if UNITY_EDITOR
            var editorScene = SceneHelper.FindSceneByName(inSceneName, SceneCategories.Build);
            string path = editorScene.Path;
            if (!editorScene.IsLoaded())
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            }
            #else
            string path = SceneHelper.FindSceneByName(inSceneName, SceneCategories.Build).Path;
            yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            #endif // UNITY_EDITOR

            SceneBinding unityScene = SceneHelper.FindSceneByPath(path, SceneCategories.Loaded);
            GameObject[] roots = unityScene.Scene.GetRootGameObjects();
            foreach(var root in roots)
            {
                SceneManager.MoveGameObjectToScene(root, inActiveScene);
            }
            yield return SceneManager.UnloadSceneAsync(unityScene);
        }

        #endregion // Scene Loading

        #region Scene History

        public void ClearSceneHistory()
        {
            m_SceneHistory.Clear();
        }

        #endregion // Scene History

        #region Scripting

        private void BindScene(SceneBinding inScene)
        {
            // table bind

            if (m_TempSceneTable == null)
            {
                m_TempSceneTable = new VariantTable("temp");
                m_TempSceneTable.Capacity = 64;

                Services.Data.BindTable("temp", m_TempSceneTable);
            }
            else
            {
                m_TempSceneTable.Clear();
            }

            // locate camera
            Services.Camera.LocateCameraRig();
            Services.UI.BindCamera(Services.Camera.Current);
        }

        #endregion // Scripting

        #region Additional Managers

        public void RegisterManager(SharedManager inManager)
        {
            Type t = inManager.GetType();
            SharedManager manager;
            if (m_SharedManagers.TryGetValue(t, out manager))
            {
                if (manager != inManager)
                    throw new ArgumentException(string.Format("Manager with type {0} already exists", t.FullName), "inManager");

                return;
            }

            m_SharedManagers.Add(t, inManager);
        }

        public void DeregisterManager(SharedManager inManager)
        {
            Type t = inManager.GetType();
            SharedManager manager;
            if (m_SharedManagers.TryGetValue(t, out manager) && manager == inManager)
            {
                m_SharedManagers.Remove(t);
            }
        }

        public T FindManager<T>() where T : SharedManager
        {
            Type t = typeof(T);
            SharedManager manager;
            if (!m_SharedManagers.TryGetValue(t, out manager))
            {
                manager = FindObjectOfType<T>();
                if (manager != null)
                {
                    RegisterManager(manager);
                }
            }
            return (T) manager;
        }

        #endregion // Additional Managers

        private void CleanupFromScene(SceneBinding inBinding, object inContext)
        {
            int removedManagerCount = 0;
            using(PooledList<SharedManager> sharedManagers = PooledList<SharedManager>.Create(m_SharedManagers.Values))
            {
                foreach(var manager in sharedManagers)
                {
                    if (manager.gameObject.scene == inBinding.Scene)
                    {
                        DeregisterManager(manager);
                        ++removedManagerCount;
                    }
                }
            }

            if (removedManagerCount > 0)
            {
                Log.Warn("[StateMgr] Unregistered {0} shared managers that were not deregistered at scene unload", removedManagerCount);
            }
        }

        #region IService

        protected override void Initialize()
        {
            m_SceneLoadRoutine.Replace(this, InitialSceneLoad());
            m_SceneLock = true;

            if (SceneHelper.ActiveScene().BuildIndex >= GameConsts.GameSceneIndexStart)
                Services.UI.ForceLoadingScreen();

            m_SharedManagers = new Dictionary<Type, SharedManager>(8);
        }

        protected override void Shutdown()
        {
            m_SceneLoadRoutine.Stop();
        }

        #endregion // IService

        #region IDebuggable

        #if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            var loadSceneMenu = new DMInfo("Load Scene", 16);

            loadSceneMenu.AddButton("Reload Current Scene", DebugReloadScene);
            loadSceneMenu.AddDivider();
            
            foreach(var scene in SceneHelper.FindScenes(SceneCategories.Build))
            {
                RegisterLoadButton(loadSceneMenu, scene);
            }

            yield return loadSceneMenu;

            var currentSceneMenu = new DMInfo("Current Scene");

            currentSceneMenu.AddButton("Dump Scene Hierarchy", DebugDumpSceneHierarchy);
            
            yield return currentSceneMenu;
        }

        static private void RegisterLoadButton(DMInfo inMenu, SceneBinding inBinding)
        {
            inMenu.AddButton(inBinding.Name, () => DebugLoadScene(inBinding));
        }

        static private void DebugLoadScene(SceneBinding inBinding)
        {
            Services.UI.HideAll();
            Services.Script.KillAllThreads();
            Services.Audio.StopAll();
            Services.State.LoadScene(inBinding);
        }

        static private void DebugReloadScene()
        {
            Services.UI.HideAll();
            Services.Script.KillAllThreads();
            Services.Audio.StopAll();
            Services.State.ReloadCurrentScene();
        }

        private struct DumpSceneHierarchyRecord
        {
            public Transform Transform;
            public int Depth;

            public DumpSceneHierarchyRecord(Transform inTransform, int inDepth)
            {
                Transform = inTransform;
                Depth = inDepth;
            }
        }

        static private void DebugDumpSceneHierarchy()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(2048);
            Scene scene = SceneHelper.ActiveScene();
            RingBuffer<DumpSceneHierarchyRecord> transformStack = new RingBuffer<DumpSceneHierarchyRecord>(32, RingBufferMode.Expand);
            GameObject[] rootObjects = scene.GetRootGameObjects();

            sb.AppendFormat("[StateMgr] Dumping scene transform hierarchy for scene '{0}'", scene.name);

            foreach(var obj in rootObjects)
            {
                transformStack.PushBack(new DumpSceneHierarchyRecord(obj.transform, 0));
            }

            transformStack.Reverse();

            DumpSceneHierarchyRecord record;
            while(transformStack.Count > 0)
            {
                record = transformStack.PopBack();
                sb.Append('\n');
                sb.Append('-', record.Depth * 3);
                sb.Append(" [");

                if (record.Transform.gameObject.activeInHierarchy)
                    sb.Append('A');
                else
                    sb.Append('-');

                if (record.Transform.gameObject.activeSelf)
                    sb.Append('a');
                else
                    sb.Append('-');

                sb.Append("] ").Append(record.Transform.gameObject.name);

                int childCount = record.Transform.childCount;
                for(int i = childCount - 1; i >= 0; i--)
                {
                    transformStack.PushBack(new DumpSceneHierarchyRecord(record.Transform.GetChild(i), record.Depth + 1));
                }
            }

            Log.Msg(sb.Flush());
        }

        #endif // DEVELOPMENT

        #endregion // IDebuggable

        #region Leaf

        [LeafMember("LoadScene")]
        static private IEnumerator LeafLoadScene(string inSceneName, StringHash32 inEntrance = default(StringHash32), string inLoadingMode = null)
        {
            SceneLoadFlags flags = SceneLoadFlags.Default;
            if (inLoadingMode == "no-loading-screen")
            {
                flags |= SceneLoadFlags.NoLoadingScreen;
            }
            
            if ((flags & SceneLoadFlags.NoLoadingScreen) != 0)
            {
                return Services.State.LoadScene(inSceneName, inEntrance, flags);
            }
            
            return StateUtil.LoadSceneWithWipe(inSceneName, inEntrance, flags);
        }

        #endregion // Leaf
    }

    public enum SceneLoadFlags
    {
        [Hidden]
        Default = 0,

        NoLoadingScreen = 0x01,
        DoNotModifyHistory = 0x02,
        Cutscene = 0x04,
        DoNotDispatchPreUnload = 0x08
    }
}