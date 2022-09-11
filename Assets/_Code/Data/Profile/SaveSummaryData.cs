using System;
using Aqua.Option;
using BeauData;
using BeauUtil;
using EasyBugReporter;

namespace Aqua.Profile {
    public struct SaveSummaryData : ISerializedObject, ISerializedVersion {
        public string Id;
        public uint ActId;
        public StringHash32 CurrentLocation;
        public StringHash32 CurrentStation;
        public SaveSummaryFlags Flags;

        #region ISerializedObject

        public ushort Version { get { return 2; } }

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.Serialize("profileId", ref Id);
            ioSerializer.Serialize("actId", ref ActId);
            ioSerializer.UInt32Proxy("currentLocation", ref CurrentLocation);
            ioSerializer.UInt32Proxy("currentStation", ref CurrentStation);
            if (ioSerializer.ObjectVersion >= 2) {
                ioSerializer.Enum("flags", ref Flags);
            }
        }

        #endregion // ISerializedObject

        static public SaveSummaryData FromSave(SaveData data) {
            SaveSummaryData summary;
            summary.Id = data.Id ?? string.Empty;
            summary.ActId = data.Script.ActIndex;
            summary.CurrentLocation = data.Map.SavedSceneId();
            summary.CurrentStation = data.Map.CurrentStationId();
            summary.Flags = GetFlags(data);
            return summary;
        }

        static public SaveSummaryFlags GetFlags(SaveData data) {
            SaveSummaryFlags flags = 0;
            if (data.Map.HasVisitedLocation(MapIds.KelpStation) || data.Jobs.IsStartedOrComplete(JobIds.Kelp_welcome) || data.Map.HasVisitedLocation(MapIds.Helm)) {
                flags |= SaveSummaryFlags.UnlockedShip;
            }
            return flags;
        }
    }

    [Flags]
    public enum SaveSummaryFlags {
        UnlockedShip = 0x01
    }
}