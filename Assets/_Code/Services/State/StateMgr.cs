using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using Aqua.Scripting;

namespace Aqua
{
    public partial class StateMgr : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private GameObject m_InitialPreloadRoot = null;

        #endregion // Inspector

        private Routine m_SceneLoadRoutine;
        [NonSerialized] private bool m_SceneLock;
        [NonSerialized] private Camera m_MainCamera;

        private VariantTable m_TempSceneTable;

        private RingBuffer<SceneBinding> m_SceneHistory = new RingBuffer<SceneBinding>(8, RingBufferMode.Overwrite);
        private Dictionary<StringHash32, SharedManager> m_SharedManagers;

        public Camera Camera { get { return m_MainCamera; } }

        #region Scene Loading

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(string inSceneName, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Debug.LogErrorFormat("[StateMgr] Scene load already in progress");
                return null;
            }

            SceneBinding scene = SceneHelper.FindSceneByName(inSceneName, SceneCategories.Build);
            if (!scene.IsValid())
            {
                Debug.LogErrorFormat("[StateMgr] No scene found with name matching '{0}'", inSceneName);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(scene, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(SceneBinding inScene, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Debug.LogErrorFormat("[StateMgr] Scene load already in progress");
                return null;
            }

            if (!inScene.IsValid())
            {
                Debug.LogErrorFormat("[StateMgr] Provided scene '{0}' is not valid", inScene);
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(inScene, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to another scene.
        /// </summary>
        public IEnumerator LoadScene(StringHash32 inSceneId, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Debug.LogErrorFormat("[StateMgr] Scene load already in progress");
                return null;
            }

            SceneBinding scene = SceneHelper.FindSceneById(inSceneId, SceneCategories.Build);
            if (!scene.IsValid())
            {
                Debug.LogErrorFormat("[StateMgr] No scene found with id '{0}'", inSceneId.ToString());
                return null;
            }

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(scene, inContext, inFlags)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Loads to the previously loaded scene.
        /// </summary>
        public IEnumerator LoadPreviousScene(string inDefault = null, object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            if (m_SceneLock)
            {
                Debug.LogErrorFormat("[StateMgr] Scene load already in progress");
                return null;
            }

            if (m_SceneHistory.Count < 2)
            {
                if (!string.IsNullOrEmpty(inDefault))
                {
                    return LoadScene(inDefault, inContext, inFlags);
                }

                Debug.LogErrorFormat("[StateMgr] No previous scene in scene history");
                return null;
            }

            // pop the current scene
            m_SceneHistory.PopBack();

            // get the previous one
            SceneBinding prevScene = m_SceneHistory.PeekBack();

            m_SceneLock = true;
            m_SceneLoadRoutine.Replace(this, SceneSwap(prevScene, inContext, inFlags | SceneLoadFlags.DoNotModifyHistory)).TryManuallyUpdate(0);
            return m_SceneLoadRoutine.Wait();
        }

        /// <summary>
        /// Reloads the current scene.
        /// </summary>
        public IEnumerator ReloadCurrentScene(object inContext = null, SceneLoadFlags inFlags = SceneLoadFlags.Default)
        {
            return LoadScene(SceneHelper.ActiveScene(), inContext, inFlags);
        }

        /// <summary>
        /// Returns if loading into another scene.
        /// </summary>
        public bool IsLoadingScene()
        {
            return m_SceneLock;
        }

        private IEnumerator InitialSceneLoad()
        {
            yield return WaitForServiceLoading();
            
            Services.Input.PauseAll();
            yield return WaitForPreload(m_InitialPreloadRoot, null);

            foreach(var obj in m_InitialPreloadRoot.GetComponentsInChildren<ISceneLoadHandler>(true))
                obj.OnSceneLoad(m_InitialPreloadRoot.scene, null);

            Debug.LogFormat("[StateMgr] Initial load of preload scene '{0}' finished", m_InitialPreloadRoot.scene.path);

            SceneBinding active = SceneManager.GetActiveScene();
            BindScene(active);
            m_SceneHistory.PushBack(active);

            yield return WaitForPreload(active, null);
            yield return WaitForServiceLoading();
            yield return WaitForCleanup();

            m_SceneLock = false;
            Services.UI.HideLoadingScreen();

            Debug.LogFormat("[StateMgr] Initial load of '{0}' finished", active.Path);
            active.BroadcastLoaded();
            Services.Input.ResumeAll();

            Services.Script.TriggerResponse(GameTriggers.SceneStart);
        }

        private IEnumerator SceneSwap(SceneBinding inNextScene, object inContext, SceneLoadFlags inFlags)
        {
            try
            {
                Services.Input.PauseAll();
                Services.Script.KillLowPriorityThreads();

                bool bShowLoading = (inFlags & SceneLoadFlags.NoLoadingScreen) == 0;
                bool bShowCutscene = (inFlags & SceneLoadFlags.Cutscene) != 0;
                if (bShowCutscene)
                {
                    Services.UI.ShowLetterbox();
                }

                if (bShowLoading)
                {
                    yield return Services.UI.ShowLoadingScreen();
                }

                SceneBinding active = SceneHelper.ActiveScene();

                // unloading instant
                Debug.LogFormat("[StateMgr] Unloading scene '{0}'", active.Path);
                active.BroadcastUnload(inContext);
                
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(inNextScene.Path, LoadSceneMode.Single);
                loadOp.allowSceneActivation = false;

                Debug.LogFormat("[StateMgr] Loading scene '{0}'", inNextScene.Path);

                while(loadOp.progress < 0.9f)
                    yield return null;

                loadOp.allowSceneActivation = true;

                while(!loadOp.isDone)
                    yield return null;

                yield return WaitForServiceLoading();
                BindScene(inNextScene);

                if ((inFlags & SceneLoadFlags.DoNotModifyHistory) == 0)
                {
                    m_SceneHistory.PushBack(inNextScene);
                }

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

                Debug.LogFormat("[StateMgr] Finished loading scene '{0}'", inNextScene.Path);
                inNextScene.BroadcastLoaded(inContext);

                Services.Script.TriggerResponse(GameTriggers.SceneStart);
            }
            finally
            {
                Services.Input.ResumeAll();
                m_SceneLock = false;
            }
        }

        private IEnumerator WaitForServiceLoading()
        {
            while(BuildInfo.IsLoading())
                yield return null;

            foreach(var service in Services.All())
            {
                if (ReferenceEquals(this, service))
                    continue;
                
                while(service.IsLoading())
                    yield return null;
            }
        }

        private IEnumerator WaitForPreload(SceneBinding inScene, object inContext)
        {
            using(PooledList<IScenePreloader> allPreloaders = PooledList<IScenePreloader>.Create())
            {
                inScene.Scene.GetAllComponents<IScenePreloader>(true, allPreloaders);
                if (allPreloaders.Count > 0)
                {
                    Debug.LogFormat("[StateMgr] Executing preload steps for scene '{0}'", inScene.Path);
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
                    Debug.LogFormat("[StateMgr] Executing preload steps for gameObject '{0}'", inRoot.FullPath(true));
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

        #endregion // Scene Loading

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

            m_MainCamera = Camera.main;
        }

        #endregion // Scripting

        #region Additional Managers

        public void RegisterManager(SharedManager inManager)
        {
            Type t = inManager.GetType();
            StringHash32 key = t.FullName;

            SharedManager manager;
            if (m_SharedManagers.TryGetValue(key, out manager))
            {
                if (manager != inManager)
                    throw new ArgumentException(string.Format("Manager with type {0} already exists", t.FullName), "inManager");

                return;
            }

            m_SharedManagers.Add(key, inManager);
        }

        public void DeregisterManager(SharedManager inManager)
        {
            Type t = inManager.GetType();
            StringHash32 key = t.FullName;

            SharedManager manager;
            if (m_SharedManagers.TryGetValue(key, out manager) && manager == inManager)
            {
                m_SharedManagers.Remove(key);
            }
        }

        public T FindManager<T>() where T : SharedManager
        {
            StringHash32 key = typeof(T).FullName;
            SharedManager manager;
            if (!m_SharedManagers.TryGetValue(key, out manager))
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
                Debug.LogWarningFormat("[StateMgr] Unregistered {0} shared managers that were not deregistered at scene unload", removedManagerCount);
            }
        }

        #region IService

        protected override void OnRegisterService()
        {
            m_SceneLoadRoutine.Replace(this, InitialSceneLoad());
            m_SceneLock = true;

            m_SharedManagers = new Dictionary<StringHash32, SharedManager>(8);
        }

        protected override void OnDeregisterService()
        {
            m_SceneLoadRoutine.Stop();
        }

        protected override bool IsLoading()
        {
            return m_SceneLoadRoutine && m_SceneLock;
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.State;
        }

        #endregion // IService
    }

    public enum SceneLoadFlags
    {
        [Hidden]
        Default = 0,

        NoLoadingScreen = 0x01,
        DoNotModifyHistory = 0x02,
        Cutscene = 0x04,
    }
}