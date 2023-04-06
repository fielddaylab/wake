using System;
using Aqua.Option;
using BeauData;
using BeauUtil;
using EasyBugReporter;

namespace Aqua.Profile {
    public struct SaveSummaryData : ISerializedObject, ISerializedVersion {
        public string Id;
        public long LastUpdated;
        public uint ActId;
        public StringHash32 CurrentLocation;
        public StringHash32 CurrentStation;
        public byte SpecterCount;
        public StringHash32 CurrentJob;
        public ushort JobCompletedCount;
        public ushort CurrentLevel;
        public SaveSummaryFlags Flags;
        public ushort DreamMask;

        #region ISerializedObject

        public ushort Version { get { return 4; } }

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.Serialize("profileId", ref Id);
            ioSerializer.Serialize("actId", ref ActId);
            ioSerializer.UInt32Proxy("currentLocation", ref CurrentLocation);
            ioSerializer.UInt32Proxy("currentStation", ref CurrentStation);
            if (ioSerializer.ObjectVersion >= 2) {
                ioSerializer.Enum("flags", ref Flags);
            }
            if (ioSerializer.ObjectVersion >= 3) {
                ioSerializer.Serialize("specters", ref SpecterCount);
                ioSerializer.Serialize("dreams", ref DreamMask);
            }
            if (ioSerializer.ObjectVersion >= 4) {
                ioSerializer.Serialize("lastUpdated", ref LastUpdated);
                ioSerializer.UInt32Proxy("currentJob", ref CurrentJob);
                ioSerializer.Serialize("jobCompletedCount", ref JobCompletedCount);
                ioSerializer.Serialize("currentLevel", ref CurrentLevel);
            }
        }

        #endregion // ISerializedObject

        static public SaveSummaryData FromSave(SaveData data) {
            SaveSummaryData summary;
            summary.Id = data.Id ?? string.Empty;
            summary.LastUpdated = data.LastUpdated;
            summary.ActId = data.Script.ActIndex;
            summary.CurrentLocation = data.Map.SavedSceneId();
            summary.CurrentStation = data.Map.CurrentStationId();
            summary.Flags = GetFlags(data);
            summary.CurrentJob = data.Jobs.CurrentJobId;
            summary.JobCompletedCount = (ushort) data.Jobs.CompletedJobIds().Count;
            summary.SpecterCount = (byte) data.Science.SpecterCount();
            summary.DreamMask = GetDreamMask(data);
            summary.CurrentLevel = (ushort) data.Science.CurrentLevel();
            return summary;
        }

        static public SaveSummaryFlags GetFlags(SaveData data) {
            SaveSummaryFlags flags = 0;
            if (data.Map.HasVisitedLocation(MapIds.KelpStation) || data.Jobs.IsStartedOrComplete(JobIds.Kelp_welcome) || data.Map.HasVisitedLocation(MapIds.Helm)) {
                flags |= SaveSummaryFlags.UnlockedShip;
            }
            if (data.Map.HasVisitedLocation(MapIds.RS_0) && data.Inventory.HasUpgrade(ItemIds.Flashlight)) {
                flags |= SaveSummaryFlags.VisitedAnglerfish;
            }
            if (data.Jobs.IsInProgress(JobIds.Final_final)) {
                flags |= SaveSummaryFlags.StartedFinalJob;
            } else if (data.Jobs.IsComplete(JobIds.Final_final)) {
                flags |= SaveSummaryFlags.CompletedFinalJob;
            }
            return flags;
        }

        static public ushort GetDreamMask(SaveData data) {
            ushort mask = 0;
            if (data.Inventory.HasJournalEntry(Dream00)) {
                mask |= 0x01;
            }
            if (data.Inventory.HasJournalEntry(Dream01)) {
                mask |= 0x02;
            }
            if (data.Inventory.HasJournalEntry(Dream02)) {
                mask |= 0x04;
            }
            if (data.Inventory.HasJournalEntry(Dream03)) {
                mask |= 0x08;
            }
            if (data.Inventory.HasJournalEntry(Dream04)) {
                mask |= 0x10;
            }
            if (data.Inventory.HasJournalEntry(Dream05)) {
                mask |= 0x20;
            }
            if (data.Inventory.HasJournalEntry(Dream06)) {
                mask |= 0x40;
            }
            if (data.Inventory.HasJournalEntry(Dream07)) {
                mask |= 0x80;
            }
            return mask;
        }

        static private readonly StringHash32 Dream00 = "Dream00_Kelp1";
        static private readonly StringHash32 Dream01 = "Dream01_WhaleFall";
        static private readonly StringHash32 Dream02 = "Dream02_Coral";
        static private readonly StringHash32 Dream03 = "Dream03_DeadZone";
        static private readonly StringHash32 Dream04 = "Dream04_Kelp2Barren";
        static private readonly StringHash32 Dream05 = "Dream05_Arctic2";
        static private readonly StringHash32 Dream06 = "Dream06_Rig";
        static private readonly StringHash32 Dream07 = "Dream07_Final";
    }

    [Flags]
    public enum SaveSummaryFlags {
        UnlockedShip = 0x01,
        VisitedAnglerfish = 0x02,
        StartedFinalJob = 0x04,
        CompletedFinalJob = 0x08
    }
}