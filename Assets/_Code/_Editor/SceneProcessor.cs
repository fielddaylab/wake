using System.Collections;
using BeauUtil;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;
using BeauUtil.Debugger;
using UnityEditor.SceneManagement;

namespace Aqua.Editor
{
    public class SceneProcessor : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (EditorApplication.isPlaying)
                return;
            
            LoadSubscenes(scene);
            RemoveBootstrap(scene);
            Flatten(scene);
            Optimize(scene);
        }

        static private void RemoveBootstrap(Scene scene)
        {
            if (scene.buildIndex > 0)
            {
                BootParams[] bootstraps = GameObject.FindObjectsOfType<BootParams>();
                if (bootstraps.Length > 0)
                {
                    Debug.LogFormat("[SceneProcessor] Removing bootstraps from scene '{0}'...", scene.name);
                    foreach(var bootstrap in bootstraps)
                    {
                        GameObject.DestroyImmediate(bootstrap.gameObject);
                    }
                }
            }
        }

        static private void LoadSubscenes(Scene scene)
        {
            List<SubScene> allSubscenes = new List<SubScene>(4);
            scene.GetAllComponents<SubScene>(false, allSubscenes);
            if (allSubscenes.Count > 0)
            {
                Debug.LogFormat("[SceneProcessor] Loading {0} subscenes in scene '{1}'...", allSubscenes.Count, scene.name);
                using(Profiling.Time("load subscenes"))
                {
                    foreach(var subscene in allSubscenes)
                    {
                        string path = subscene.Scene.Path;
                        GameObject.DestroyImmediate(subscene.gameObject);
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        Scene unitySubScene = EditorSceneManager.GetSceneByPath(path);
                        foreach(var root in unitySubScene.GetRootGameObjects())
                        {
                            EditorSceneManager.MoveGameObjectToScene(root, scene);
                        }
                        EditorSceneManager.CloseScene(unitySubScene, true);
                    }
                }
            }
        }

        static private void Flatten(Scene scene)
        {
            List<FlattenHierarchy> allFlatten = new List<FlattenHierarchy>(64);
            scene.GetAllComponents<FlattenHierarchy>(false, allFlatten);
            if (allFlatten.Count > 0)
            {
                Debug.LogFormat("[SceneProcessor] Flattening {0} transform hierarchies in scene '{1}'...", allFlatten.Count, scene.name);
                using(Profiling.Time("flatten scene hierarchy"))
                {
                    foreach(var flatten in allFlatten)
                        flatten.Flatten();
                }
            }
        }

        static private void Optimize(Scene scene)
        {
            List<ISceneOptimizable> allOptimizable = new List<ISceneOptimizable>();
            scene.GetAllComponents<ISceneOptimizable>(true, allOptimizable);
            if (allOptimizable.Count > 0)
            {
                Debug.LogFormat("[SceneProcessor] Optimizing {0} objects scene '{1}'...", allOptimizable.Count, scene.name);
                using(Profiling.Time("optimizing objects"))
                {
                    foreach(var optimizable in allOptimizable)
                    {
                        Debug.LogFormat("[SceneProcessor] ...optimizing {0}", optimizable.ToString());
                        optimizable.Optimize();
                    }
                }
            }
        }
    }
}