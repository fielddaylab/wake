// comment out this define to disable the AB test
#define ANALYTICS_ABTEST_USERCODEREMINDER

using System.Collections;
using Aqua.Profile;
using BeauRoutine;
using UnityEngine;

namespace Aqua.Analytics {
    static public class UserCodeReminderFeature {
        public enum Status {
            Invalid,
            Inactive,
            Active
        }

        static public Status GetStatus(SaveData saveData) {
#if ANALYTICS_ABTEST_USERCODEREMINDER
            if (saveData == null || saveData.IsBookmark || string.IsNullOrEmpty(saveData.Id)) {
                return Status.Inactive;
            }

            char c = saveData.Id[0];
            int offset = char.ToUpperInvariant(c) - 'A';
            return (offset & 0x1) == 0 ? Status.Active : Status.Inactive;
#else
            return Status.Inactive;
#endif // ANALYTICS_ABTEST_USERCODEREMINDER
        }

        static public string GetModifiedBranchName(string branchName, Status active) {
            switch (active) {
                case Status.Invalid:
                default:
                    return branchName;
                case Status.Inactive:
                    return branchName + "-no-savecode-reminder";
                case Status.Active:
                    return branchName + "-has-savecode-reminder";
            }
        }

        static public void TryQueueDisplay() {
            var status = GetStatus(Save.Current);
            if (status == Status.Active) {
                Services.State.OnSceneLoadReady(DisplayReminder);
            }
        }

        static private IEnumerator DisplayReminder() {
            LoadingIcon.Cancel();

            UserCodeReminderPopup popup = GameObject.Instantiate(Resources.Load<UserCodeReminderPopup>("Prefabs/UserCodeReminderPopup"));
            popup.Group.alpha = 0;
            popup.Button.interactable = false;
            popup.Group.blocksRaycasts = false;
            popup.UserCode.SetText(Save.Current.Id);
            yield return CanvasExtensions.Show(popup.Group, 0.25f, true);
            yield return 2;
            popup.Button.interactable = true;
            yield return popup.Button.onClick.WaitForInvoke();

            popup.Group.blocksRaycasts = false;

            yield return CanvasExtensions.Hide(popup.Group, 0.25f);

            GameObject.Destroy(popup.gameObject);

            LoadingIcon.Queue();
        }
    }
}
