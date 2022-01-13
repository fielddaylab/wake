using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using BeauUtil.Editor;
using Leaf;
using Aqua.Scripting;
using BeauUtil;
using System.Text;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.IO;
using UnityEditor.SceneManagement;
using BeauUtil.Debugger;

namespace Aqua.Editor
{
    static public class PrefabTools
    {
        [MenuItem("Aqualab/Clean Camera")]
        static public void CleanCameraData()
        {
            foreach(var obj in Selection.gameObjects)
            {
                var cameraData = obj.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.hideFlags = HideFlags.HideAndDontSave;
                    GameObject.DestroyImmediate(cameraData);
                }
            }
        }

        [MenuItem("Aqualab/Delete Missing Components")]
        static public void CleanMissingComponents()
        {
            CleanMissingComponents(Selection.gameObjects);
        }

        [MenuItem("Aqualab/Delete Missing Components From All Scenes")]
        static public void CleanMissingComponentsFromAllScenes()
        {
            string currentPath = EditorSceneManager.GetActiveScene().path;
            List<SceneBinding> allScenes = new List<SceneBinding>(SceneHelper.AllBuildScenes(true));
            try
            {
                Log.Msg("[PrefabTools] Removing missing components in all build scenes");
                for(int i = 0; i < allScenes.Count; i++)
                {
                    SceneBinding scene = allScenes[i];
                    EditorUtility.DisplayProgressBar("Removing Missing Components", string.Format("{0} ({1}/{2})", scene.Name, i + 1, allScenes.Count), (float) i / allScenes.Count);
                    Log.Msg("[PrefabTools] Loading '{0}'", scene.Path);
                    EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Single);
                    CleanMissingComponents(Resources.FindObjectsOfTypeAll<GameObject>());
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

        static private bool CleanMissingComponents(GameObject[] objects)
        {
            List<Component> components = new List<Component>();
            int affected = 0;
            var deep = DeepHierarchy(objects);

            foreach(var go in deep)
            {
                string fullPath = null;
                int missingComponents = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingComponents > 0)
                {
                    Undo.RegisterCompleteObjectUndo(go, "Removing missing scripts");
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (removed > 0)
                    {
                        Debug.LogFormat("Deleted {0} missing components from GameObject {1}", missingComponents, fullPath ?? (fullPath = UnityHelper.FullPath(go, true)));
                        affected++;
                    }
                    else
                    {
                        Debug.LogWarningFormat("Missing components detected but not deleted...");
                    }
                }
            }

            return affected > 0;
        }

        static private HashSet<GameObject> DeepHierarchy(GameObject[] objects)
        {
            HashSet<GameObject> deepSet = new HashSet<GameObject>();
            foreach(var obj in objects)
            {
                DeepHierarchy(obj, deepSet);
            }
            return deepSet;
        }

        static private void DeepHierarchy(GameObject root, HashSet<GameObject> deepSet)
        {
            deepSet.Add(root);
            foreach(Transform transform in root.transform)
            {
                DeepHierarchy(transform.gameObject, deepSet);
            }
        }

        [MenuItem("Aqualab/Refresh Databases")]
        static public void RefreshAllDBs()
        {
            DBObject.RefreshCollection<MapDesc, MapDB>();
            DBObject.RefreshCollection<BestiaryDesc, BestiaryDB>();
            DBObject.RefreshCollection<JobDesc, JobDB>();
            DBObject.RefreshCollection<ActDesc, ActDB>();
            DBObject.RefreshCollection<WaterPropertyDesc, WaterPropertyDB>();
            DBObject.RefreshCollection<ScriptCharacterDef, ScriptCharacterDB>();
            DBObject.RefreshCollection<InvItem, InventoryDB>();
        }
    }
}