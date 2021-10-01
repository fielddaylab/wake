using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental;
#endif // UNITY_EDITOR

namespace Aqua
{
    [ExecuteAlways]
    public sealed class NoOverridesAllowed : MonoBehaviour, ISceneOptimizable
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

        void ISceneOptimizable.Optimize()
        {
            DestroyImmediate(this);
        }

        #endif // UNITY_EDITOR
    }
}