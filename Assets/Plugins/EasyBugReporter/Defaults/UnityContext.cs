using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace EasyBugReporter {

    /// <summary>
    /// Reports loaded scenes and gathers reports from IReportSource components.
    /// </summary>
    public class UnityContext : IDumpSystem {
        public bool Dump(IDumpWriter writer) {
            writer.BeginSection("Loaded Scenes");

            writer.KeyValue("Time Since Startup", Time.realtimeSinceStartup.ToString() + " seconds");
            writer.KeyValue("Profiler Memory Usage Report", Profiler.GetTotalAllocatedMemoryLong());
            
            HashSet<IDumpSource> unityReporters = new HashSet<IDumpSource>();
            List<IDumpSource> tempUnityReporters = new List<IDumpSource>(64);
            int sceneCount = SceneManager.sceneCount;
            for(int i = 0; i < sceneCount; i++) {
                Scene scene = SceneManager.GetSceneAt(i);
                writer.KeyValue("Loaded Scene", scene.path);
            }

            writer.KeyValue("Current Resolution", string.Format("{0}x{1} full={2}", Screen.width, Screen.height, Screen.fullScreen));
            writer.KeyValue("Current Framerate Target", Application.targetFrameRate);

            writer.EndSection();

            writer.BeginSection("World");

            foreach(var obj in GameObject.FindObjectsOfType<GameObject>()) {
                obj.GetComponents<IDumpSource>(tempUnityReporters);
                foreach(var reporter in tempUnityReporters) {
                    unityReporters.Add(reporter);
                }
            }

            foreach(var reportContext in unityReporters) {
                if (!reportContext.GetType().IsDefined(typeof(AlwaysReportAttribute), true)) {
                    Component c = reportContext as Component;
                    Behaviour b = c as Behaviour;
                    if (b != null && !b.isActiveAndEnabled) {
                        continue;
                    } else if (c != null && !c.gameObject.activeInHierarchy) {
                        continue;
                    }
                }

                try {
                    reportContext.Dump(writer);
                }
                catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            }

            writer.EndSection();

            return true;
        }

        public void Initialize() {
        }

        public void Shutdown() {
        }

        public void Freeze() {
        }

        public void Unfreeze() {
        }
    }

    /// <summary>
    /// Apply this attribute to a MonoBehaviour class that inherits from IReportSource
    /// to allow it to report when the component or GameObject is inactive.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AlwaysReportAttribute : Attribute {
    }
}