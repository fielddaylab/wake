using System;
using System.Diagnostics;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [DebuggerDisplay("{ToDebugString()}")]
    public struct JobTaskKey : IEquatable<JobTaskKey>, ISerializedObject
    {
        public StringHash32 JobId;
        public StringHash32 TaskId;

        public JobTaskKey(StringHash32 inJobId, StringHash32 inTaskId)
        {
            JobId = inJobId;
            TaskId = inTaskId;
        }

        public override bool Equals(object obj)
        {
            if (obj is JobTaskKey)
                return Equals((JobTaskKey) obj);
            return false;
        }

        public string ToDebugString()
        {
            return string.Format("[{0}:{1}]", JobId.ToDebugString(), TaskId.ToDebugString());
        }

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", JobId.ToString(), TaskId.ToString());
        }

        public bool Equals(JobTaskKey other)
        {
            return JobId == other.JobId && TaskId == other.TaskId;
        }

        public override int GetHashCode()
        {
            return (int) (JobId.HashValue << 4 ^ TaskId.HashValue);
        }

        #region ISerializedObject

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("jobId", ref JobId);
            ioSerializer.Serialize("taskId", ref TaskId);
        }

        #endregion // ISerializedObject
    }
}