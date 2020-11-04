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

namespace ProtoAqua.Editor
{
    static public class BuildSettings
    {
        [InitializeOnLoadMethod]
        static private void Setup()
        {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;
        }

        [MenuItem("Aqualab/Log Git Branch", false, 1001)]
        static private void LogGitBranch()
        {
            string branch = BuildUtils.GetSourceControlBranchName();
            Debug.LogFormat("[BuildSettings] Source control branch is '{0}'", branch);
        }

        private class BuildPreprocess : IPreprocessBuildWithReport
        {
            public int callbackOrder { get { return 99; } }

            public void OnPreprocessBuild(BuildReport report)
            {
                // TODO: More sophisticated mechanism for controlling development, defines, other flags...
                string branch = BuildUtils.GetSourceControlBranchName();
                EditorUserBuildSettings.development = false;
                
                if (branch != null)
                {
                    if (branch.Contains("dev") || branch.Contains("proto"))
                        EditorUserBuildSettings.development = true;
                }
            }
        }
    }
}