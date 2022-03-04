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
using System;

namespace Aqua.Editor
{
    static public class BuildSettings
    {
        private const string EditorPrefsAutoOptimizeOnPlayKey = "_Aqualab_AutoOptimizeOnPlay";

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

            PlayerSettings.SetManagedStrippingLevel(EditorUserBuildSettings.selectedBuildTargetGroup, bDesiredDevBuild ? ManagedStrippingLevel.Medium : ManagedStrippingLevel.High);
            EditorApplication.playModeStateChanged += OnPlayStateChanged;
        }

        static private bool IsAutoOptimizeEnabled()
        {
            return EditorPrefs.GetBool(EditorPrefsAutoOptimizeOnPlayKey, true);
        }

        static private void OnPlayStateChanged(PlayModeStateChange inChange)
        {
            if (inChange != PlayModeStateChange.ExitingEditMode)
                return;

            if (IsAutoOptimizeEnabled())
            {
                BakeAllAssets();
            }
        }

        [MenuItem("Optimize/Run %#o", false, 1)]
        static private void BakeAllAssets()
        {
            List<IBakedAsset> allBaked = new List<IBakedAsset>(512);
            using(Profiling.Time("bake assets"))
            {
                foreach(var asset in AssetDBUtils.FindAssets<ScriptableObject>())
                {
                    IBakedAsset baked;
                    if ((baked = asset as IBakedAsset) != null)
                    {
                        allBaked.Add(baked);
                    }

                    // ensure ids are reversible from hash
                    DBObject dbObj;
                    BFBase fact;
                    if ((dbObj = asset as DBObject) != null)
                    {
                        dbObj.Id();
                    }
                    if ((fact = asset as BFBase) != null)
                    {
                        new StringHash32(fact.name);
                    }
                }

                allBaked.Sort((a, b) => a.Order.CompareTo(b.Order));
                int count = allBaked.Count;
                int current = 0;
                ScriptableObject scriptableAsset;
                try
                {
                    foreach(var baked in allBaked)
                    {
                        scriptableAsset = (ScriptableObject) baked;
                        EditorUtility.DisplayProgressBar("Baking Scriptable Objects", string.Format("{0} ({1}/{2})", scriptableAsset.name, current + 1, count), current / (float) count);
                        if (baked.Bake())
                        {
                            EditorUtility.SetDirty(scriptableAsset);
                            Debug.LogFormat("[BuildSettings] Baked asset '{0}' of type '{1}'", scriptableAsset.name, baked.GetType().Name);
                        }
                        current++;
                    }

                    AssetDatabase.SaveAssets();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        [MenuItem("Optimize/Auto Enable", false, 20)]
        static private void EnableAutoOptimization()
        {
            EditorPrefs.SetBool(EditorPrefsAutoOptimizeOnPlayKey, true);
        }

        [MenuItem("Optimize/Auto Disable", false, 21)]
        static private void DisableAutoOptimization()
        {
            EditorPrefs.SetBool(EditorPrefsAutoOptimizeOnPlayKey, false);
        }

        [MenuItem("Optimize/Auto Enable", true)]
        static private bool EnableAutoOptimization_Enabled()
        {
            return !IsAutoOptimizeEnabled();
        }

        [MenuItem("Optimize/Auto Disable", true)]
        static private bool DisableAutoOptimization_Enabled()
        {
            return IsAutoOptimizeEnabled();
        }

        static private void StripEditorInfoFromAssets()
        {
            List<IEditorOnlyData> allStrippable = new List<IEditorOnlyData>(512);
            using(Profiling.Time("strip scriptable objects"))
            {
                foreach(var asset in AssetDBUtils.FindAssets<ScriptableObject>())
                {
                    IEditorOnlyData strippable;
                    if ((strippable = asset as IEditorOnlyData) != null)
                    {
                        allStrippable.Add(strippable);
                    }
                }

                int count = allStrippable.Count;
                int current = 0;
                ScriptableObject scriptableAsset;
                try
                {
                    foreach(var strippable in allStrippable)
                    {
                        scriptableAsset = (ScriptableObject) strippable;
                        EditorUtility.DisplayProgressBar("Stripping Editor Data", string.Format("{0} ({1}/{2})", scriptableAsset.name, current + 1, count), current / (float) count);
                        strippable.ClearEditorOnlyData();
                        EditorUtility.SetDirty(scriptableAsset);
                        Debug.LogFormat("[BuildSettings] Stripped editor-only data from asset '{0}' of type '{1}'", scriptableAsset.name, strippable.GetType().Name);
                        current++;
                    }

                    AssetDatabase.SaveAssets();
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
                bool bBatch = UnityEditorInternal.InternalEditorUtility.inBatchMode;
                if (bBatch)
                {
                    PlayerSettings.SplashScreen.show = false;
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                }
                Debug.LogFormat("[BuildSettings] Building branch '{0}', development mode {1}", branch, EditorUserBuildSettings.development);
                try
                {
                    BakeAllAssets();
                    if (bBatch) {
                        CodeGen.GenerateJobsConsts();
                        NoOverridesAllowed.RevertInAllScenes();
                        StripEditorInfoFromAssets();
                    }
                }
                catch(Exception e)
                {
                    throw new BuildFailedException(e);
                }
            }
        }
    }
}