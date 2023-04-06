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
using BeauData;
using Aqua.Profile;
using ScriptableBake;
using Aqua.Journal;

namespace Aqua.Editor
{
    static public class PrefabTools
    {
        [MenuItem("Aqualab/Clean Camera")]
        static public void CleanCameraData()
        {
            foreach(var obj in Selection.gameObjects)
            {
                var cameraDatas = obj.GetComponents<UniversalAdditionalCameraData>();
                if (cameraDatas.Length > 1)
                {
                    for(int i = 1; i < cameraDatas.Length; i++) {
                        UniversalAdditionalCameraData data = cameraDatas[i];
                        data.hideFlags = HideFlags.HideAndDontSave;
                        GameObject.DestroyImmediate(data);
                    }
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

        [MenuItem("Optimize/Rebuild Databases", false, 50)]
        static public void RefreshAllDBs()
        {
            DBObject.RefreshCollection<JournalDesc, JournalDB>();
            DBObject.RefreshCollection<MapDesc, MapDB>();
            DBObject.RefreshCollection<BestiaryDesc, BestiaryDB>();
            DBObject.RefreshCollection<JobDesc, JobDB>();
            DBObject.RefreshCollection<ActDesc, ActDB>();
            DBObject.RefreshCollection<WaterPropertyDesc, WaterPropertyDB>();
            DBObject.RefreshCollection<ScriptCharacterDef, ScriptCharacterDB>();
            DBObject.RefreshCollection<InvItem, InventoryDB>();
        }

        [MenuItem("Optimize/Bake Selection", false, 51)]
        static private void BakeAllAssets()
        {
            using(Profiling.Time("bake assets"))
            {
                using(Log.DisableMsgStackTrace())
                {
                    Baking.BakeObjects(Selection.objects, BakeFlags.Verbose | BakeFlags.ShowProgressBar);
                }
            }
            using(Profiling.Time("post-bake save assets"))
            {
                AssetDatabase.SaveAssets();
            }
        }

        static public void ConvertToBeauDataBinary<T>(TextAsset asset, bool rename) where T : ISerializedObject
        {
            T data = Serializer.Read<T>(asset);
            string path = AssetDatabase.GetAssetPath(asset);
            string outputPath = Path.ChangeExtension(path, ".bytes");
            if (rename) {
                string error = AssetDatabase.MoveAsset(path, outputPath);
                if (!string.IsNullOrEmpty(error)) {
                    Log.Error(error);
                    return;
                }
            }
            Serializer.WriteFile(data, outputPath, OutputOptions.None, Serializer.Format.Binary);
            AssetDatabase.ImportAsset(outputPath);
            Log.Msg("[PrefabTools] Compressed file '{0}' to '{1}'", path, outputPath);
        }
        
        [MenuItem("Aqualab/Test Compress Selection")]
        static public void AttemptCompress()
        {
            foreach(var file in Selection.objects) {
                LZCompress(file);
            }
        }

        static public void LZCompress(UnityEngine.Object asset) {
            string path = AssetDatabase.GetAssetPath(asset);
            string outputPath = Path.ChangeExtension(path, ".lzb");
            byte[] read = File.ReadAllBytes(path);
            byte[] compressed;
            using(Profiling.Time("compressing file")) {
                compressed = UnsafeExt.Compress(read);
            }
            File.WriteAllBytes(outputPath, compressed);
            Log.Msg("[PrefabTools] Compressed file '{0}' with ratio {1}", path, (float) read.Length / compressed.Length);
            EditorUtility.RevealInFinder(outputPath);
        }

        [MenuItem("Aqualab/Align to Surface")]
        static public void AlignToSurface() {
            foreach(var obj in Selection.gameObjects) {
                Transform t = obj.transform;
                Ray r = new Ray(t.position + t.forward * 0.01f, t.forward);
                Debug.DrawRay(r.origin, r.direction * 10, Color.red, 1);
                bool hit = Physics.Raycast(r, out RaycastHit hitInfo);
                if (hit) {
                    Undo.RecordObject(t, "Aligning with surface");
                    t.position = hitInfo.point;
                    t.forward = -hitInfo.normal;
                    Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.blue, 2);
                }
            }
        }

        // static private PropertyModification[] s_CopiedOverrides;

        // [MenuItem("Aqualab/Prefab Overrides/Copy Prefab Modifications", false, 90)]
        // static public void CopyPrefabModifications() {
        //     s_CopiedOverrides = PrefabUtility.GetPropertyModifications(Selection.activeGameObject);
        // }

        // [MenuItem("Aqualab/Prefab Overrides/Copy Prefab Modifications", true, 90)]
        // static private bool CopyPrefabModifications_Validate() {
        //     if (!Selection.activeGameObject) {
        //         return false;
        //     }

        //     return PrefabUtility.IsPartOfPrefabInstance(Selection.activeGameObject);
        // }

        // [MenuItem("Aqualab/Prefab Overrides/Paste Prefab Modifications", false, 100)]
        // static public void PastePrefabModifications() {
        //     SerializedObject obj = new SerializedObject(Selection.activeGameObject);
            
        // }

        // [MenuItem("Aqualab/Prefab Overrides/Paste Prefab Modifications", true, 100)]
        // static private bool PastePrefabModifications_Validate() {
        //     if (s_CopiedOverrides == null || s_CopiedOverrides.Length == 0) {
        //         return false;
        //     }

        //     if (!Selection.activeGameObject) {
        //         return false;
        //     }

        //     return PrefabUtility.IsPartOfPrefabInstance(Selection.activeGameObject);
        // }

        [MenuItem("Aqualab/Align GameObject with Collider Offset")]
        static public void AlignObjectWithColliderOffset() {
            foreach(var obj in Selection.gameObjects) {
                Transform t = obj.transform;
                Collider2D c = obj.GetComponent<Collider2D>();
                if (!c) {
                    continue;
                }

                Undo.RecordObject(t, "Adjusting collider offset");
                Undo.RecordObject(c, "Adjusting collider offset");

                t.localPosition += (Vector3) c.offset;
                c.offset = default(Vector2);

                EditorUtility.SetDirty(t);
                EditorUtility.SetDirty(c);
            }
        }

        [MenuItem("Aqualab/Prefabs/Revert All In Selection")]
        static public void RevertAllInSelection() {
            RevertAll(Selection.gameObjects, false);
        }

        [MenuItem("Aqualab/Prefabs/Revert All In Selection (Recursive)")]
        static public void RevertAllInSelectionRecursive() {
            RevertAll(Selection.gameObjects, true);
        }

        static public void RevertAll(GameObject[] gos, bool recursive) {
            HashSet<UnityEngine.Object> toRevert = new HashSet<UnityEngine.Object>();
            foreach(var go in gos) {
                GatherRevertObjects(go, toRevert, recursive);
            }
            int count = toRevert.Count;
            int idx = 0;
            try {
                foreach(var revert in toRevert) {
                    try {
                        idx++;
                        if (EditorUtility.DisplayCancelableProgressBar("Reverting Objects", revert.name, (float) idx / count)) {
                            return;
                        }
                        PrefabUtility.RevertObjectOverride(revert, InteractionMode.UserAction);
                    } catch { }
                }
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        static private List<Component> s_ScratchComponentList = new List<Component>(8);

        static public void GatherRevertObjects(GameObject root, HashSet<UnityEngine.Object> objects, bool recursive) {
            if (objects.Add(root)) {
                root.GetComponents<Component>(s_ScratchComponentList);
                foreach(var obj in s_ScratchComponentList) {
                    objects.Add(obj);
                }

                if (recursive) {
                    foreach(Transform t in root.transform) {
                        GatherRevertObjects(t.gameObject, objects, recursive);
                    }
                }
            }
        }
    }
}