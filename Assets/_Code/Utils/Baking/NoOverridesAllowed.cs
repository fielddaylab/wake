using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil;
using System.Collections.Generic;
using BeauUtil.Debugger;
using ScriptableBake;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace Aqua
{
    [ExecuteAlways]
    public sealed class NoOverridesAllowed : MonoBehaviour, IBaked
    {
        #if UNITY_EDITOR

        private bool TryRevert()
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(this))
                return false;
            
            GameObject prefabRoot;
            if (prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject))
            {
                if (PrefabUtility.HasPrefabInstanceAnyOverrides(prefabRoot, true))
                {
                    PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
                    EditorUtility.SetDirty(prefabRoot);
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Aqualab/Revert Critical Prefab Overrides")]
        static internal void RevertAll()
        {
            foreach(var obj in FindObjectsOfType<NoOverridesAllowed>())
            {
                if (obj.TryRevert())
                {
                    Debug.LogFormat("[NoOverridesAllowed] Reverted '{0}'", obj.name);
                }
            }
        }

        [MenuItem("Aqualab/Revert Critical Prefab Overrides In All Scenes")]
        static internal void RevertInAllScenes()
        {
            string currentPath = EditorSceneManager.GetActiveScene().path;
            List<SceneBinding> allScenes = new List<SceneBinding>(SceneHelper.AllBuildScenes(true));
            try
            {
                Log.Msg("[NoOverridesAllowed] Reverting critical prefab overrides in all build scenes");
                for(int i = 0; i < allScenes.Count; i++)
                {
                    SceneBinding scene = allScenes[i];
                    EditorUtility.DisplayProgressBar("Clearing Critical Prefab Overrides", string.Format("{0} ({1}/{2})", scene.Name, i + 1, allScenes.Count), (float) i / allScenes.Count);
                    Log.Msg("[NoOverridesAllowed] Loading '{0}'", scene.Path);
                    EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Single);
                    RevertAll();
                    EditorSceneManager.SaveOpenScenes();
                }
            }
            finally
            {
                if (!UnityEditorInternal.InternalEditorUtility.inBatchMode) {
                    EditorSceneManager.OpenScene(currentPath, OpenSceneMode.Single);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        int IBaked.Order { get { return FlattenHierarchy.Order - 100; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            Baking.Destroy(this);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}