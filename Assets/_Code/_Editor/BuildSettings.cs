using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aqua.Profile;
using Aqua.Scripting;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using Leaf;
using Leaf.Runtime;
using ScriptableBake;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Aqua.Editor {
    static public class BuildSettings {
        private const string EditorPrefsAutoOptimizeOnPlayKey = "_Aqualab_AutoOptimizeOnPlay";

        [InitializeOnLoadMethod]
        static private void Setup() {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;

            Bake.OnPreBake += (b) => {
                ScriptableObject scr = b as ScriptableObject;
                if (scr) {
                    new StringHash32(scr.name);
                }
            };

            // TODO: More sophisticated mechanism for controlling development, defines, other flags...
            string branch = BuildUtils.GetSourceControlBranchName();
            bool bDesiredDevBuild = false;
            bool bDesiredPreviewBuild = false;
            bool bDesiredProdBuild = false;

            if (branch != null) {
                if (branch.StartsWith("feature/") || branch.StartsWith("fix/") || branch.StartsWith("improvement/") || branch.StartsWith("experimental/")
                    || branch.Contains("dev") || branch.Contains("proto")) {
                    bDesiredDevBuild = true;
                } else if (branch.StartsWith("milestone") || branch.Contains("preview")) {
                    bDesiredPreviewBuild = true;
                } else if (branch.StartsWith("production")) {
                    bDesiredProdBuild = true;
                }
            }

            bool bApply = bDesiredDevBuild != EditorUserBuildSettings.development || UnityEditorInternal.InternalEditorUtility.inBatchMode;

            if (bApply) {
                EditorUserBuildSettings.development = bDesiredDevBuild;
                Debug.LogFormat("[BuildSettings] Source control branch is '{0}', switched development build to {1}", branch, bDesiredDevBuild);
            }

            if (bDesiredDevBuild) {
                BuildUtils.WriteDefines("DEVELOPMENT");
            } else if (bDesiredPreviewBuild) {
                BuildUtils.WriteDefines("PREVIEW,ENABLE_LOGGING_ERRORS_BEAUUTIL,ENABLE_LOGGING_WARNINGS_BEAUUTIL,PRESERVE_DEBUG_SYMBOLS");
            } else {
                BuildUtils.WriteDefines("PRODUCTION");
            }

            PlayerSettings.SetManagedStrippingLevel(EditorUserBuildSettings.selectedBuildTargetGroup, bDesiredDevBuild ? ManagedStrippingLevel.Medium : ManagedStrippingLevel.High);
            EditorApplication.playModeStateChanged += OnPlayStateChanged;
        }

        static private bool IsAutoOptimizeEnabled() {
            return EditorPrefs.GetBool(EditorPrefsAutoOptimizeOnPlayKey, true);
        }

        static private void OnPlayStateChanged(PlayModeStateChange inChange) {
            if (inChange != PlayModeStateChange.ExitingEditMode)
                return;

            if (IsAutoOptimizeEnabled()) {
                BakeAllAssets();
            }
        }

        [MenuItem("Optimize/Run %#o", false, 1)]
        static private void BakeAllAssets() {
            using (Profiling.Time("bake assets")) {
                using (Log.DisableMsgStackTrace()) {
                    Bake.Assets(BakeFlags.Verbose | BakeFlags.ShowProgressBar);
                }
            }
            using (Profiling.Time("post-bake save assets")) {
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Optimize/Auto Enable", false, 20)]
        static private void EnableAutoOptimization() {
            EditorPrefs.SetBool(EditorPrefsAutoOptimizeOnPlayKey, true);
        }

        [MenuItem("Optimize/Auto Disable", false, 21)]
        static private void DisableAutoOptimization() {
            EditorPrefs.SetBool(EditorPrefsAutoOptimizeOnPlayKey, false);
        }

        [MenuItem("Optimize/Auto Enable", true)]
        static private bool EnableAutoOptimization_Enabled() {
            return !IsAutoOptimizeEnabled();
        }

        [MenuItem("Optimize/Auto Disable", true)]
        static private bool DisableAutoOptimization_Enabled() {
            return IsAutoOptimizeEnabled();
        }

        [MenuItem("Aqualab/DEBUG/Compress Bookmarks")]
        static private void CompressBookmarks() {
            foreach (TextAsset asset in AssetDBUtils.FindAssets<TextAsset>(null, new string[] { "Assets/Resources/Bookmarks" })) {
                PrefabTools.ConvertToBeauDataBinary<SaveData>(asset, true);
            }
        }

        [MenuItem("Aqualab/Leaf/Validate All Scripts")]
        static private void DEBUGValidateAllScripts() {
            ValidateAllScripts();
        }

        static public bool ValidateAllScripts() {
            try {
                ScriptNodePackage.Generator generator = new ScriptNodePackage.Generator(Leaf.Compiler.LeafCompilerFlags.Default_Strict);
                MethodCache<LeafMember> methodCache = LeafUtils.CreateMethodCache(typeof(IScriptComponent));
                methodCache.LoadStatic();
                foreach(var type in Reflect.FindDerivedTypes(typeof(IScriptComponent), Reflect.FindAllUserAssemblies())) {
                    methodCache.Load(type);
                }

                bool hasErrors = false;

                var allScripts = AssetDBUtils.FindAssets<LeafAsset>();
                int idx = 0;

                foreach(var asset in allScripts)
                {
                    idx++;
                    if (asset.name.Contains(".template")) {
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(asset);

                    EditorUtility.DisplayProgressBar("Compiling all leaf scripts", assetPath, idx / (float) allScripts.Length);

                    ScriptNodePackage package = LeafAsset.Compile<ScriptNode, ScriptNodePackage>(asset, generator);
                    var errorState = package.ErrorState();
                    if (errorState.ErrorMask != 0) {
                        Debug.LogErrorFormat("Leaf Script '{0}' has compilation errors - see previous error log for detail", assetPath);
                        hasErrors = true;
                    }
                }

                return !hasErrors;
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }

        static private void StripEditorInfoFromAssets() {
            List<IEditorOnlyData> allStrippable = new List<IEditorOnlyData>(512);
            using (Profiling.Time("strip scriptable objects")) {
                foreach (var asset in AssetDBUtils.FindAssets<ScriptableObject>()) {
                    IEditorOnlyData strippable;
                    if ((strippable = asset as IEditorOnlyData) != null) {
                        allStrippable.Add(strippable);
                    }
                }

                int count = allStrippable.Count;
                int current = 0;
                ScriptableObject scriptableAsset;
                try {
                    foreach (var strippable in allStrippable) {
                        scriptableAsset = (ScriptableObject)strippable;
                        EditorUtility.DisplayProgressBar("Stripping Editor Data", string.Format("{0} ({1}/{2})", scriptableAsset.name, current + 1, count), current / (float)count);
                        strippable.ClearEditorOnlyData();
                        EditorUtility.SetDirty(scriptableAsset);
                        Debug.LogFormat("[BuildSettings] Stripped editor-only data from asset '{0}' of type '{1}'", scriptableAsset.name, strippable.GetType().Name);
                        current++;
                    }

                    AssetDatabase.SaveAssets();
                } finally {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private class BuildPreprocess : IPreprocessBuildWithReport {
            public int callbackOrder { get { return 99; } }

            public void OnPreprocessBuild(BuildReport report) {
                string branch = BuildUtils.GetSourceControlBranchName();
                bool bBatch = UnityEditorInternal.InternalEditorUtility.inBatchMode;
                if (bBatch) {
                    PlayerSettings.SplashScreen.show = false;
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                    PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                }
                Debug.LogFormat("[BuildSettings] Building branch '{0}', development mode {1}", branch, EditorUserBuildSettings.development);
                try {
                    using (Profiling.Time("bake assets"))
                    using (Log.DisableMsgStackTrace()) {
                        Bake.Assets(bBatch ? 0 : BakeFlags.Verbose);
                    }
                    AssetDatabase.SaveAssets();
                    if (!ValidateAllScripts()) {
                        throw new Exception("Invalid scripts present");
                    }
                    if (bBatch) {
                        CodeGen.GenerateJobsConsts();
                        NoOverridesAllowed.RevertInAllScenes();
                        StripEditorInfoFromAssets();
                        CompressBookmarks();
                    }
                } catch (Exception e) {
                    throw new BuildFailedException(e);
                }
            }
        }
    }
}