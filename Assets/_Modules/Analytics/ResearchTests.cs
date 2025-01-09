// Comment out the line below to disallow any AB testing
#define ABTESTS_ALLOWED

#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Runtime.CompilerServices;
using Aqua.Profile;
using OGD;

namespace Aqua.Analytics {
    static public class ResearchTests {
        #region Tests

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int FirstCharOffset(char c) {
            return (char.ToUpperInvariant(c) - 'A');
        }

        static public bool IsAB(SaveData save, int index) {
#if ABTESTS_ALLOWED
#if DEVELOPMENT
            if (s_DEBUGForceTest >= 0) {
                return (s_DEBUGForceTest & 0x1) == index;
            }
#endif // DEVELOPMENT

            if (save == null || save.IsBookmark || string.IsNullOrEmpty(save.Id)) {
                return false;
            }

            return (FirstCharOffset(save.Id[0]) & 0x1) == index;
#else
            return false;
#endif // ABTESTS_ALLOWED
        }

        static public bool IsABC(SaveData save, int index) {
#if ABTESTS_ALLOWED
#if DEVELOPMENT
            if (s_DEBUGForceTest >= 0) {
                return (s_DEBUGForceTest % 3) == index;
            }
#endif // DEVELOPMENT

            if (save == null || save.IsBookmark || string.IsNullOrEmpty(save.Id)) {
                return false;
            }

            return (FirstCharOffset(save.Id[0]) % 3) == index;
#else
            return false;
#endif // ABTESTS_ALLOWED
        }

        static public bool IsFlagged(SaveData save, int flag) {
#if ABTESTS_ALLOWED
            return save != null && save.ResearchFlags.IsSet(flag);
#else
            return false;
#endif // ABTESTS_ALLOWED
        }

        #endregion // Tests

        #region Modifying Branch Name

        static public string GetModifiedBranchName(string originalBranchName) {
#if ABTESTS_ALLOWED
            string branch = originalBranchName;
            branch = UserCodeReminderFeature.GetModifiedBranchName(branch, UserCodeReminderFeature.GetStatus(Save.Current));
            branch = JobPredictionFeature.GetModifiedBranchName(branch, JobPredictionFeature.GetStatus(Save.Current));
            branch = AlternateJobGraphFeature.GetModifiedBranchName(branch, AlternateJobGraphFeature.GetStatus(Save.Current));
            return branch;
#else
            return originalBranchName;
#endif // ABTESTS_ALLOWED
        }

        #endregion // Modifying Branch Name

#if DEVELOPMENT
        static internal int s_DEBUGForceTest = -1;

        static internal void DEBUGRefreshAllTests() {
#if ABTESTS_ALLOWED
            JobPredictionFeature.TryLoadTable();
            AlternateJobGraphFeature.TryApplyPatch();
#endif // ABTESTS_ALLOWED
        }
#endif // DEVELOPMENT

        static public void HandleProfileStart(OGDSurvey surveyDisplayer) {
#if ABTESTS_ALLOWED
            if (IsNewSave(Save.Current)) {
                Services.State.OnSceneLoadReady(() => InitialSurvey(surveyDisplayer));
            } else {
                UserCodeReminderFeature.TryQueueDisplay();
            }
            JobPredictionFeature.TryLoadTable();
            AlternateJobGraphFeature.TryApplyPatch();
#endif // ABTESTS_ALLOWED
        }

        static public void HandleProfileEnd() {
            ContentPatcher.Undo();
        }

        static private bool IsNewSave(SaveData saveData) {
            return saveData.LaunchCount == 1 && saveData.Script.ProfileNodeHistory.Count == 0;
        }

        static private IEnumerator InitialSurvey(OGDSurvey survey) {
            survey.TryDisplaySurvey("first-launch");
            while(Services.UI.IsDisplayingSurvey) {
                yield return null;
            }
        }
    }
}