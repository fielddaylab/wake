using System;
using BeauData;
using BeauUtil;

namespace Aqua {
    public struct PlayerJob : ISerializedObject {
        public StringHash32 JobId;
        public JobStatusFlags Status;

        [NonSerialized] public JobDesc Job;

        public PlayerJob(StringHash32 inJobId, JobStatusFlags inStatus, JobDesc inDesc) {
            JobId = inJobId;
            Status = inStatus;
            Job = inDesc;
        }

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.UInt32Proxy("id", ref JobId);
            ioSerializer.Enum("status", ref Status);
        }

        public bool IsValid {
            get { return !JobId.IsEmpty; }
        }

        static public JobProgressCategory StatusToCategory(JobStatusFlags inStatus) {
            if ((inStatus & JobStatusFlags.Completed) != 0)
                return JobProgressCategory.Completed;
            if ((inStatus & JobStatusFlags.Active) != 0)
                return JobProgressCategory.Active;
            if ((inStatus & JobStatusFlags.InProgress) != 0)
                return JobProgressCategory.InProgress;
            if ((inStatus & JobStatusFlags.Visible) != 0) {
                if ((inStatus & JobStatusFlags.Unlocked) == 0)
                    return JobProgressCategory.Locked;
                return JobProgressCategory.Available;
            }

            return JobProgressCategory.Hidden;
        }
    }

    [Flags]
    public enum JobStatusFlags : byte {
        Hidden = 0,
        Visible = 0x01,
        Unlocked = 0x02,
        InProgress = 0x04,
        Active = 0x08,
        Completed = 0x10,

        [Hidden] Mask_Progress = InProgress | Active | Completed,
        [Hidden] Mask_Available = Visible | Unlocked,

        [Hidden] Default_InProgress = Mask_Available | InProgress,
        [Hidden] Default_Active = Mask_Available | InProgress | Active,
        [Hidden] Default_Completed = Mask_Available | Completed,
    }

    public enum JobProgressCategory : byte {
        Active,
        InProgress,
        Completed,
        Available,
        Locked,
        Hidden
    }
}