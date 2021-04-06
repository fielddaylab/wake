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

namespace Aqua.Editor
{
    public class SceneProcessor : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            RemoveBootstrap(scene);
            Flatten(scene);
        }

        static private void RemoveBootstrap(Scene scene)
        {
            if (scene.buildIndex > 0)
            {
                BootParams bootstrap = GameObject.FindObjectOfType<BootParams>();
                if (bootstrap != null)
                {
                    Debug.LogFormat("[SceneProcessor] Removing bootstrap from scene '{0}'...", scene.name);
                    GameObject.DestroyImmediate(bootstrap.gameObject);
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
    }
}