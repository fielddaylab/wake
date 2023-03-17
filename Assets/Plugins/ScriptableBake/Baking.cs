using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif // UNITY_EDITOR

namespace ScriptableBake {

    /// <summary>
    /// Object baking utility.
    /// </summary>
    static public class Baking {

        public delegate void BakeDelegate(IBaked baked);

        #if UNITY_EDITOR

        /// <summary>
        /// Executes prior to any object baking.
        /// </summary>
        static public event BakeDelegate OnPreBake;

        /// <summary>
        /// Executes after all object baking.
        /// </summary>
        static public event BakeDelegate OnPostBake;

        #region Scene

        /// <summary>
        /// Bakes the current scene.
        /// </summary>
        static public void BakeCurrentScene(BakeFlags flags = 0) {
            BakeScene(SceneManager.GetActiveScene(), flags);
        }

        /// <summary>
        /// Bakes scene components.
        /// </summary>
        static public void BakeScene(Scene scene, BakeFlags flags = 0) {
            IEnumerator iter = BakeSceneAsync(scene, flags);
            using (iter as IDisposable) {
                while (iter.MoveNext()) ;
            }
        }

        /// <summary>
        /// Bakes the current scene asynchronously.
        /// Use this in a coroutine.
        /// </summary>
        static public IEnumerator BakeCurrentSceneAsync(BakeFlags flags = 0) {
            return BakeSceneAsync(SceneManager.GetActiveScene(), flags);
        }

        /// <summary>
        /// Bakes scene asynchronously.
        /// Use this in a coroutine.
        /// </summary>
        static public IEnumerator BakeSceneAsync(Scene scene, BakeFlags flags = 0) {
            bool bIgnoreDisabled = (flags & BakeFlags.IgnoreDisabledObjects) != 0;

            List<IBaked> rootLocal = new List<IBaked>(16);
            GameObject[] roots = scene.GetRootGameObjects();
            List<IBaked> bakeComponents = new List<IBaked>(roots.Length * 4);
            for (int i = 0; i < roots.Length; i++) {
                GameObject root = roots[i];
                if (bIgnoreDisabled && !root.activeSelf) {
                    continue;
                }

                rootLocal.Clear();
                root.GetComponentsInChildren<IBaked>(!bIgnoreDisabled, rootLocal);
                bakeComponents.AddRange(rootLocal);
            }

            BakeContext context = new BakeContext();
            context.Scene = SceneManager.GetActiveScene();
            context.MainCamera = FindMainCamera();
            context.HasFog = RenderSettings.fog;
            if (context.HasFog) {
                context.FogStartDistance = RenderSettings.fogStartDistance;
                context.FogEndDistance = RenderSettings.fogEndDistance;
            }
            context.m_Flags = flags;
            return Process(bakeComponents, "scene: " + scene.name, flags, context, null);
        }

        static private Camera FindMainCamera() {
            var cameras = GameObject.FindObjectsOfType<Camera>();
            Camera bestMask = null;
            int bestMaskCount = 0;
            foreach(var camera in cameras) {
                if (camera.CompareTag("MainCamera")) {
                    return camera;
                }

                int maskCount = CountCameraMask(camera.cullingMask);
                if (maskCount > bestMaskCount) {
                    bestMaskCount = maskCount;
                    bestMask = camera;
                }
            }

            return bestMask;
        }

        static private unsafe int CountCameraMask(int eventMask) {
            uint unsigned = *(uint*)(&eventMask);
            int count = 0;
            while(unsigned != 0)
            {
                count += (int) (unsigned & 1);
                unsigned >>= 1;
            }
            return count;
        }

        #endregion // Scene

        #region Assets

        /// <summary>
        /// Bakes custom assets.
        /// </summary>
        static public void BakeAssets(BakeFlags flags = 0) {
            IEnumerator iter = BakeAssetsAsync(flags);
            using (iter as IDisposable) {
                while (iter.MoveNext()) ;
            }
        }

        /// <summary>
        /// Bakes custom assets within the given directories.
        /// </summary>
        static public void BakeAssets(string[] directories, BakeFlags flags = 0) {
            IEnumerator iter = BakeAssetsAsync(directories, flags);
            using (iter as IDisposable) {
                while (iter.MoveNext()) ;
            }
        }

        /// <summary>
        /// Bakes custom assets asynchronously.
        /// Use this in a coroutine.
        /// </summary>
        static public IEnumerator BakeAssetsAsync(BakeFlags flags = 0) {
            return BakeAssetsAsync(null, flags);
        }

        /// <summary>
        /// Bakes custom assets within the given directories asynchronously.
        /// Use this in a coroutine.
        /// </summary>
        static public IEnumerator BakeAssetsAsync(string[] directories, BakeFlags flags = 0) {
            string[] guids;
            if (directories != null && directories.Length > 0) {
                guids = AssetDatabase.FindAssets("t:ScriptableObject", directories);
            } else {
                guids = AssetDatabase.FindAssets("t:ScriptableObject");
            }

            List<IBaked> bakeAssets = new List<IBaked>(guids.Length);
            for (int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] objectsAtPath = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objectsAtPath) {
                    IBaked baked = obj as IBaked;
                    if (baked != null) {
                        bakeAssets.Add(baked);
                    }
                }
            }

            return Process(bakeAssets, "ScriptableObjects", flags, default(BakeContext), null);
        }

        #endregion // Assets

        #region Manual Selection

        /// <summary>
        /// Bakes a list of objects.
        /// </summary>
        static public void BakeObjects(IReadOnlyList<UnityEngine.Object> objects, BakeFlags flags = 0) {
            IEnumerator iter = BakeObjectsAsync(objects, flags);
            using (iter as IDisposable) {
                while (iter.MoveNext()) ;
            }
        }

        /// <summary>
        /// Bakes a list of objects asynchronously.
        /// Use this in a coroutine.
        /// </summary>
        static public IEnumerator BakeObjectsAsync(IReadOnlyList<UnityEngine.Object> objects, BakeFlags flags = 0) {
            List<IBaked> bakeAssets = new List<IBaked>(objects.Count);
            for (int i = 0; i < objects.Count; i++) {
                IBaked baked = objects[i] as IBaked;
                if (baked != null) {
                    bakeAssets.Add(baked);
                }
            }

            return Process(bakeAssets, "Objects", flags, default(BakeContext), null);
        }

        #endregion // Manual Selection

        #region Prefabs

        // static public IEnumerator PrefabsAsync(string[] directories, BakeFlags flags = 0) {
        //     string[] guids;
        //     if (directories != null && directories.Length > 0) {
        //         guids = AssetDatabase.FindAssets("t:Prefab", directories);
        //     } else {
        //         guids = AssetDatabase.FindAssets("t:Prefab");
        //     }
        // }

        #endregion // Prefabs

        #region Hierarchy

        // Brought over from BeauUtil

        /// <summary>
        /// Flattens the hierarchy at this transform. Children will become siblings.
        /// </summary>
        static public void FlattenHierarchy(Transform transform, bool destroyInactive, bool recursive = false, bool ignoreAnimators = true) {
            if (recursive) {
                int placeIdx = transform.GetSiblingIndex() + 1;
                FlattenHierarchyRecursive(transform, transform.parent, destroyInactive, ignoreAnimators, ref placeIdx);
                return;
            }

            if (!Application.isPlaying) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            if (ignoreAnimators && transform.GetComponent<Animator>()) {
                return;
            }

            Transform parent = transform.parent;
            Transform child;
            int childCount = transform.childCount;
            int siblingIdx = transform.GetSiblingIndex() + 1;
            while (childCount-- > 0) {
                child = transform.GetChild(0);
                if (destroyInactive && !child.gameObject.activeSelf) {
                    GameObject.DestroyImmediate(child.gameObject);
                } else {
                    child.SetParent(parent, true);
                    child.SetSiblingIndex(siblingIdx++);
                }
            }
        }

        static private void FlattenHierarchyRecursive(Transform transform, Transform parent, bool destroyInactive, bool ignoreAnimators, ref int siblingIndex) {
            if (!Application.isPlaying) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            if (ignoreAnimators && transform.GetComponent<Animator>()) {
                return;
            }

            Transform child;
            int childCount = transform.childCount;
            while (childCount-- > 0) {
                child = transform.GetChild(0);
                if (destroyInactive && !child.gameObject.activeSelf) {
                    GameObject.DestroyImmediate(child.gameObject);
                } else {
                    child.SetParent(parent, true);
                    child.SetSiblingIndex(siblingIndex++);
                    FlattenHierarchyRecursive(child, parent, destroyInactive, ignoreAnimators, ref siblingIndex);
                }
            }
        }

        static private List<Component> s_CachedComponentList;

        /// <summary>
        /// Returns if the given Transform is a leaf node in its transform hierarchy,
        /// and has no non-transform components.
        /// </summary>
        static public bool IsEmptyLeaf(Transform transform) {
            if (transform.childCount > 0) {
                return false;
            }

            List<Component> tempList = s_CachedComponentList ?? (s_CachedComponentList = new List<Component>(4));
            transform.gameObject.GetComponents<Component>(tempList);
            int count = tempList.Count;
            tempList.Clear();
            return count == 1; // transform is included, so must be more than 1
        }

        #endregion // Hierarchy

        #region Static Flags

        #if UNITY_EDITOR

        public delegate StaticEditorFlags ModifyStaticFlagsDelegate(StaticEditorFlags current);

        /// <summary>
        /// Resets the static editor flags for a given hierarchy.
        /// </summary>
        static public void ResetStaticFlags(GameObject go, bool recursive = false) {
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            if (recursive) {
                SetStaticFlagsRecursive(go, 0);
            }
        }

        /// <summary>
        /// Sets the static editor flags for a given hierarchy.
        /// </summary>
        static public void SetStaticFlags(GameObject go, StaticEditorFlags flags, bool recursive = false) {
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            if (recursive) {
                SetStaticFlagsRecursive(go, flags);
            }
        }

        /// <summary>
        /// Adds the static editor flags for a given hierarchy.
        /// </summary>
        static public void AddStaticFlags(GameObject go, StaticEditorFlags flags, bool recursive = false) {
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(go) | flags);
            if (recursive) {
                AddStaticFlagsRecursive(go, flags);
            }
        }

        /// <summary>
        /// Removes the static editor flags for a given hierarchy.
        /// </summary>
        static public void RemoveStaticFlags(GameObject go, StaticEditorFlags flags, bool recursive = false) {
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(go) & ~flags);
            if (recursive) {
                RemoveStaticFlagsRecursive(go, flags);
            }
        }

        /// <summary>
        /// Modifies the static editor flags for a given hierarchy.
        /// </summary>
        static public void ModifyStaticFlags(GameObject go, ModifyStaticFlagsDelegate modifier, bool recursive = false) {
            GameObjectUtility.SetStaticEditorFlags(go, modifier(GameObjectUtility.GetStaticEditorFlags(go)));
            if (recursive) {
                ModifyStaticFlagsRecursive(go, modifier);
            }
        }

        static private void SetStaticFlagsRecursive(GameObject go, StaticEditorFlags flags) {
            Transform transform = go.transform;
            if (!Application.isPlaying) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            GameObject child;
            int childCount = transform.childCount;
            for(int i = 0; i < childCount; i++) {
                child = transform.GetChild(i).gameObject;
                GameObjectUtility.SetStaticEditorFlags(child, flags);
                SetStaticFlagsRecursive(child, flags);
            }
        }

        static private void AddStaticFlagsRecursive(GameObject go, StaticEditorFlags flags) {
            Transform transform = go.transform;
            if (!Application.isPlaying) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            GameObject child;
            int childCount = transform.childCount;
            for(int i = 0; i < childCount; i++) {
                child = transform.GetChild(i).gameObject;
                GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(child) | flags);
                AddStaticFlagsRecursive(child, flags);
            }
        }

        static private void RemoveStaticFlagsRecursive(GameObject go, StaticEditorFlags flags) {
            Transform transform = go.transform;
            if (!Application.isPlaying) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            GameObject child;
            int childCount = transform.childCount;
            for(int i = 0; i < childCount; i++) {
                child = transform.GetChild(i).gameObject;
                GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(child) & ~flags);
                RemoveStaticFlagsRecursive(child, flags);
            }
        }

        static private void ModifyStaticFlagsRecursive(GameObject go, ModifyStaticFlagsDelegate modifier) {
            Transform transform = go.transform;
            if (!Application.isPlaying) {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            GameObject child;
            int childCount = transform.childCount;
            for(int i = 0; i < childCount; i++) {
                child = transform.GetChild(i).gameObject;
                GameObjectUtility.SetStaticEditorFlags(child, modifier(GameObjectUtility.GetStaticEditorFlags(child)));
                ModifyStaticFlagsRecursive(child, modifier);
            }
        }


        #endif // UNITY_EDITOR

        #endregion // Static Flags

        /// <summary>
        /// Destroys an object.
        /// </summary>
        static public void Destroy(UnityEngine.Object obj) {
            if (!Application.isPlaying) {
                #if UNITY_EDITOR
                if (obj is GameObject) {
                    GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                    if (prefabRoot != null) {
                        PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                } else if (obj is Component) {
                    SetDirty(((Component) obj).gameObject);
                }
                #endif // UNITY_EDITOR
                GameObject.DestroyImmediate(obj);
            } else {
                GameObject.Destroy(obj);
            }
        }

        static public void SetDirty(UnityEngine.Object obj) {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
            #endif // UNITY_EDITOR
        }

        static private IEnumerator Process(List<IBaked> baked, string source, BakeFlags flags, BakeContext context, Action<UnityEngine.Object> onModify) {
            bool bVerbose = (flags & BakeFlags.Verbose) != 0;
            bool bProgress = (flags & BakeFlags.ShowProgressBar) != 0;
            bool bError = false;

            if (Application.isPlaying) {
                flags |= BakeFlags.IsRuntime;
            }

            #if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer) {
                flags |= BakeFlags.IsBuild;
            } else {
                flags |= BakeFlags.InEditor;
            }
            if (!InternalEditorUtility.isHumanControllingUs || InternalEditorUtility.inBatchMode) {
                flags |= BakeFlags.IsBatchMode;
            }
            if (EditorUserBuildSettings.development) {
                flags |= BakeFlags.IsDevelopment;
            }
            #endif // UNITY_EDITOR

            if (context == null) {
                context = new BakeContext();
                context.Scene = SceneManager.GetActiveScene();
                context.m_Flags = flags;
            }

            baked.Sort(SortByOrder);

            if (bVerbose) {
                Debug.LogFormat("[Bake] Found {0} bakeable objects in {1}", baked.Count, source);
            }

            try {
                if (baked.Count > 0) {
                    if (OnPreBake != null) {
                        if (bProgress) {
                            if (EditorUtility.DisplayCancelableProgressBar("Baking objects", "Pre-bake step", 0)) {
                                yield break;
                            }
                        }
                        foreach (var component in baked) {
                            OnPreBake(component);
                            yield return null;
                        }
                    }

                    for(int i = 0; i < baked.Count; i++) {
                        IBaked bakedObj = baked[i];
                        UnityEngine.Object unityObj = bakedObj as UnityEngine.Object;
                        if (bProgress) {
                            if (EditorUtility.DisplayCancelableProgressBar("Baking objects", string.Format("Baking '{0}'", bakedObj.ToString()), (float) i / baked.Count)) {
                                yield break;
                            }
                        }
                        if (bVerbose) {
                            Debug.LogFormat("[Bake] ...baking '{0}'", bakedObj.ToString());
                        }

                        try {
                            if (!object.ReferenceEquals(unityObj, null) && unityObj == null) {
                                if (bVerbose) {
                                    Debug.LogFormat("[Bake] Object was destroyed");
                                }
                            } else if (bakedObj.Bake(flags, context)) {
                                if (unityObj) {
                                    EditorUtility.SetDirty(unityObj);
                                    onModify?.Invoke(unityObj);
                                    if (bVerbose) {
                                        Debug.LogFormat("[Bake] baked changes to '{0}'", bakedObj.ToString());
                                    }
                                } else {
                                    baked.RemoveAt(i--);
                                }
                            }
                        }
                        catch(Exception e) {
                            Debug.LogException(e);
                            bError = true;
                        }
                        yield return null;
                        int old = baked.Count;
                        if (context.DequeueAdditionalBakes(baked)) {
                            if (OnPreBake != null) {
                                for(int j = old; j < baked.Count; j++) {
                                    OnPreBake(baked[j]);
                                    yield return null;
                                }
                            }

                            baked.Sort(i + 1, baked.Count - i - 1, SortByOrder_Comparer.Instance);
                        }
                    }

                    if (OnPostBake != null) {
                        if (bProgress) {
                            if (EditorUtility.DisplayCancelableProgressBar("Baking objects", "Post-bake step", 1)) {
                                yield break;
                            }
                        }
                        foreach (var component in baked) {
                            OnPostBake(component);
                            yield return null;
                        }
                    }
                }
            } finally {
                if (bProgress) {
                    EditorUtility.ClearProgressBar();
                }
            }

            if (bError) {
                throw new BakeException("Baking failed");
            }
        }

        static private readonly Comparison<IBaked> SortByOrder = (a, b) => a.Order.CompareTo(b.Order);
        private class SortByOrder_Comparer : IComparer<IBaked> {
            static public readonly SortByOrder_Comparer Instance = new SortByOrder_Comparer();
            public int Compare(IBaked x, IBaked y)
            {
                return x.Order.CompareTo(y.Order);
            }
        }

#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Exception indicating that baking failed at some point.
    /// </summary>
    public class BakeException : Exception {
        public BakeException(Exception inner)
            : base("Baking failed", inner)
        { }

        public BakeException(string msg)
            : base(msg)
        { }

        public BakeException(string msg, params object[] args)
            : base(string.Format(msg, args))
        { }
    }

    /// <summary>
    /// Identifies a bakeable component or asset.
    /// </summary>
    public interface IBaked {
        #if UNITY_EDITOR
        int Order { get; }
        bool Bake(BakeFlags flags, BakeContext context);
        #endif // UNITY_EDITOR
    }

    /// <summary>
    /// Flags to modify bake behavior.
    /// </summary>
    public enum BakeFlags {

        // Disabled scene objects will be ignored.
        IgnoreDisabledObjects = 0x01,

        // This is for a build
        IsBuild = 0x02,

        // This is for a batch-mode build
        IsBatchMode = 0x04,

        // This will output info to debug
        Verbose = 0x08,

        // This will display a progress bar
        ShowProgressBar = 0x10,

        // Baking is occurring at runtime
        IsRuntime = 0x20,

        // Is this a development build
        IsDevelopment = 0x40,

        // Is this in a non-build editor environment
        InEditor = 0x80,
    }

    /// <summary>
    /// Bake context.
    /// </summary>
    public class BakeContext {
        /// <summary>
        /// Scene reference.
        /// </summary>
        public Scene Scene;

        /// <summary>
        /// Scene main camera.
        /// </summary>
        public Camera MainCamera;

        /// <summary>
        /// If fog is enabled.
        /// </summary>
        public bool HasFog;

        /// <summary>
        /// Fog start distance.
        /// </summary>
        public float FogStartDistance;

        /// <summary>
        /// Fog end distance.
        /// </summary>
        public float FogEndDistance;

        internal BakeFlags m_Flags;
        private Dictionary<string, object> m_ValueCache;
        internal List<IBaked> m_AdditionalBakeQueue; 

        /// <summary>
        /// Returns if a value with the given id is cached.
        /// </summary>
        public bool IsCached(string id) {
            return m_ValueCache != null && m_ValueCache.ContainsKey(id);
        }

        /// <summary>
        /// Caches the given value.
        /// </summary>
        public void Cache(string id, object value) {
            if (m_ValueCache == null) {
                m_ValueCache = new Dictionary<string, object>(StringComparer.Ordinal);
            }
            m_ValueCache[id] = value;
        }

        /// <summary>
        /// Retrieves a value from the cache.
        /// </summary>
        public T FromCache<T>(string id) {
            object val;
            if (m_ValueCache == null || !m_ValueCache.TryGetValue(id, out val)) {
                throw new KeyNotFoundException("no key with id " + id);
            }
            return (T) val;
        }

        /// <summary>
        /// Queues up an additional bake.
        /// </summary>
        public void QueueAdditionalBake(GameObject root) {
            bool bIgnoreDisabled = (m_Flags & BakeFlags.IgnoreDisabledObjects) != 0;

            if (bIgnoreDisabled && !root.activeSelf) {
                return;
            }

            List<IBaked> bakeComponents = new List<IBaked>(4);
            root.GetComponentsInChildren<IBaked>(!bIgnoreDisabled, bakeComponents);
            if (m_AdditionalBakeQueue == null) {
                m_AdditionalBakeQueue = new List<IBaked>(8);
            }
            m_AdditionalBakeQueue.AddRange(bakeComponents);
        }

        internal bool DequeueAdditionalBakes(List<IBaked> dest) {
            if (m_AdditionalBakeQueue != null && m_AdditionalBakeQueue.Count > 0) {
                dest.AddRange(m_AdditionalBakeQueue);
                m_AdditionalBakeQueue.Clear();
                return true;
            }

            return false;
        }
    }
}