using System.Collections;
using BeauUtil;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Collections.Generic;
using BeauUtil.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using BeauUtil.Debugger;

namespace Aqua.Editor
{
    static public class BuildSettings
    {
        [InitializeOnLoadMethod]
        static private void Setup()
        {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;

            // TODO: More sophisticated mechanism for controlling development, defines, other flags...
            string branch = BuildUtils.GetSourceControlBranchName();
            bool bDesiredDevBuild = false;
            
            if (branch != null)
            {
                if (branch.StartsWith("feature/") || branch.StartsWith("fix/") || branch.StartsWith("improvement/")
                    || branch.Contains("dev") || branch.Contains("proto"))
                {
                    bDesiredDevBuild = true;
                }
            }

            bool bApply = bDesiredDevBuild != EditorUserBuildSettings.development || UnityEditorInternal.InternalEditorUtility.inBatchMode;

            if (bApply)
            {
                EditorUserBuildSettings.development = bDesiredDevBuild;
                Debug.LogFormat("[BuildSettings] Source control branch is '{0}', switched development build to {1}", branch, bDesiredDevBuild);

                if (bDesiredDevBuild)
                {
                    BuildUtils.WriteDefines("DEVELOPMENT");
                }
                else
                {
                    BuildUtils.WriteDefines(null);
                }
            }

            EditorApplication.playModeStateChanged += OnPlayStateChanged;
        }

        static private void OnPlayStateChanged(PlayModeStateChange inChange)
        {
            if (inChange != PlayModeStateChange.ExitingEditMode)
                return;

            OptimizeAllAssets();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Aqualab/Optimize Assets")]
        static private void OptimizeAllAssets()
        {
            List<IOptimizableAsset> allOptimizable = new List<IOptimizableAsset>(512);
            using(Profiling.Time("optimize scriptable objects"))
            {
                foreach(var asset in AssetDBUtils.FindAssets<ScriptableObject>())
                {
                    IOptimizableAsset optimizable;
                    if ((optimizable = asset as IOptimizableAsset) != null)
                    {
                        allOptimizable.Add(optimizable);
                    }
                }

                allOptimizable.Sort((a, b) => a.Order.CompareTo(b.Order));
                int count = allOptimizable.Count;
                int current = 0;
                ScriptableObject scriptableAsset;
                try
                {
                    foreach(var optimizable in allOptimizable)
                    {
                        scriptableAsset = (ScriptableObject) optimizable;
                        EditorUtility.DisplayProgressBar("Optimizing Scriptable Objects", string.Format("{0} ({1}/{2})", scriptableAsset.name, current + 1, count), current / (float) count);
                        if (optimizable.Optimize())
                        {
                            EditorUtility.SetDirty(scriptableAsset);
                            Debug.LogFormat("[BuildSettings] Optimized asset '{0}' of type '{1}'", scriptableAsset.name, optimizable.GetType().Name);
                        }
                        current++;
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private class BuildPreprocess : IPreprocessBuildWithReport
        {
            public int callbackOrder { get { return 99; } }

            public void OnPreprocessBuild(BuildReport report)
            {
                string branch = BuildUtils.GetSourceControlBranchName();
                Debug.LogFormat("[BuildSettings] Building branch '{0}', development mode {1}", branch, EditorUserBuildSettings.development);
                OptimizeAllAssets();
            }
        }
    }
}