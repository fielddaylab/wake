#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

// comment out this define to disable the AB test
#define ANALYTICS_ABTEST_ALTJOBGRAPH

using System;
using BeauUtil;
using Aqua.Profile;
using System.Collections.Generic;
using UnityEngine;
using BeauUtil.Debugger;
using BeauRoutine;
using System.Collections;

namespace Aqua.Analytics {
    static public class AlternateJobGraphFeature {
        private const string PatchFilePath = "Research/AltJobGraphPatch";

        public enum Status {
            Invalid,
            Inactive,
            Active
        }

        static public Status GetStatus(SaveData saveData) {
#if ANALYTICS_ABTEST_ALTJOBGRAPH
            return ResearchTests.IsABC(saveData, 2) ? Status.Active : Status.Inactive;
#else
            return Status.Invalid;
#endif // ANALYTICS_ABTEST_ALTJOBGRAPH
        }

        static public void TryApplyPatch() {
#if ANALYTICS_ABTEST_ALTJOBGRAPH
            if (GetStatus(Save.Current) != Status.Active) {
                ContentPatcher.Undo();
                return;
            }

            if (ContentPatcher.IsPatched()) {
                return;
            }

            TextAsset patchFile = Resources.Load<TextAsset>(PatchFilePath);
            ContentPatcher.Apply(patchFile.text);
            Resources.UnloadAsset(patchFile);
#endif // ANALYTICS_ABTEST_ALTJOBGRAPH
        }

        static public string GetModifiedBranchName(string branchName, Status active) {
            switch (active) {
                case Status.Invalid:
                default:
                    return branchName;
                case Status.Inactive:
                    return branchName + "-original-job-graph";
                case Status.Active:
                    return branchName + "-alt-job-graph";
            }
        }
    }
}