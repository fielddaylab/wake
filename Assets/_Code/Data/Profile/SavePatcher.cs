using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;

namespace Aqua.Profile {
    static public class SavePatcher {
        public const uint CurrentVersion = 3;

        static private Dictionary<StringHash32, StringHash32> s_RenameSet = new Dictionary<StringHash32, StringHash32>(32);

        static public bool TryPatch(SaveData ioData) {
            if (ioData.Version == CurrentVersion)
                return false;

            Patch(ioData);
            ioData.Version = CurrentVersion;
            return true;
        }

        static private void Patch(SaveData ioData) {
            if (ioData.Version == 0) {
                UpgradeFromVersion0(ioData);
                UpgradeFromVersion1(ioData);
                UpgradeFromVersion2(ioData);
            } else if (ioData.Version == 1) {
                UpgradeFromVersion1(ioData);
                UpgradeFromVersion2(ioData);
            } else if (ioData.Version == 2) {
                UpgradeFromVersion2(ioData);
            }
        }

        static private void UpgradeFromVersion0(SaveData ioData) {
            // ioData.Map.TimeMode = TimeMode.FreezeAt12;
            // ioData.Map.CurrentTime = new GTDate(12, 0, 0);

            foreach (var diveSite in Services.Assets.Map.DiveSites()) {
                if (diveSite.HasFlags(MapFlags.UnlockedByDefault))
                    ioData.Map.UnlockSite(diveSite.Id());
            }
        }

        static private void UpgradeFromVersion1(SaveData ioData) {

        }

        static private void UpgradeFromVersion2(SaveData ioData) {
            if (ioData.Jobs.CurrentJobId == JobIds.Kelp_welcome && ioData.Jobs.IsTaskComplete("returnToShip")) {
                ioData.Map.UnlockRoom("Experimentation");
                ioData.Inventory.AddUpgrade("ObservationTank");
            }
        }

        #region Patching Ids

        static public void InitializeIdPatcher(TextAsset asset) {
            StringSlice file = asset.text;
            // TODO: handle case of rename [a -> b] and [b -> c] to only emit [a -> c]
            foreach(var line in file.EnumeratedSplit(StringUtils.DefaultNewLineChars, System.StringSplitOptions.RemoveEmptyEntries)) {
                TagData tag = TagData.Parse(line, TagStringParser.CurlyBraceDelimiters);
                s_RenameSet.Add(tag.Id, tag.Data);
            }
        }

        static public bool PatchId(ref StringHash32 ioId) {
            if (s_RenameSet.TryGetValue(ioId, out StringHash32 replace)) {
                ioId = replace;
                return true;
            }

            return false;
        }

        static public bool PatchIds(HashSet<StringHash32> ioSet) {
            if (ioSet.Count == 0) {
                return false;
            }

            bool changed = false;
            foreach(var pair in s_RenameSet) {
                if (ioSet.Remove(pair.Key)) {
                    ioSet.Add(pair.Value);
                    changed = true;
                }
            }

            return changed;
        }

        static public bool PatchIds(List<StringHash32> ioList) {
            if (ioList.Count == 0) {
                return false;
            }

            bool changed = false;
            for(int i = 0; i < ioList.Count; i++) {
                StringHash32 item = ioList[i];
                if (PatchId(ref item)) {
                    ioList[i] = item;
                    changed = true;
                }
            }

            return changed;
        }

        static public bool PatchIds(RingBuffer<StringHash32> ioList) {
            if (ioList.Count == 0) {
                return false;
            }

            bool changed = false;
            for(int i = 0; i < ioList.Count; i++) {
                ref StringHash32 item = ref ioList[i];
                changed |= PatchId(ref item);
            }

            return changed;
        }

        static public bool PatchIds(StringHash32[] ioList) {
            if (ioList.Length == 0) {
                return false;
            }

            bool changed = false;
            for(int i = 0; i < ioList.Length; i++) {
                StringHash32 item = ioList[i];
                if (PatchId(ref item)) {
                    ioList[i] = item;
                    changed = true;
                }
            }

            return changed;
        }

        #endregion // Patching Ids
    }
}