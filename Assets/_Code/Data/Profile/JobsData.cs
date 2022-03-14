#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile {
    public class JobsData : IProfileChunk, ISerializedVersion, ISerializedCallbacks {
        // Serialized
        private StringHash32 m_CurrentJobId;
        private HashSet<StringHash32> m_InProgressJobs = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_CompletedJobs = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UnlockedJobs = new HashSet<StringHash32>();
        private HashSet<JobTaskKey> m_CompletedTasks = new HashSet<JobTaskKey>();

        // Non-Serialized
        private PlayerJob m_CurrentJob;
        private HashSet<StringHash32> m_CurrentJobTaskIds = new HashSet<StringHash32>();
        private bool m_HasChanges;

        #region Current Job

        public PlayerJob CurrentJob { get { return m_CurrentJob; } }
        public StringHash32 CurrentJobId { get { return m_CurrentJobId; } }

        public bool SetCurrentJob(StringHash32 inJobId) {
            if (m_CurrentJobId == inJobId)
                return false;

            Assert.False(m_CompletedJobs.Contains(inJobId), "Cannot use completed job as active job");

            m_HasChanges = true;

            if (!m_CurrentJobId.IsEmpty)
                Services.Events.Dispatch(GameEvents.JobUnload, inJobId);

            m_CurrentJobId = inJobId;
            m_CurrentJobTaskIds.Clear();

            Services.Events.Dispatch(GameEvents.JobPreload, inJobId);

            if (inJobId == StringHash32.Null) {
                m_CurrentJob = default;
                Services.Events.Dispatch(GameEvents.JobSwitched, StringHash32.Null);
                return true;
            }

            bool bCreated = m_InProgressJobs.Add(inJobId);
            m_CurrentJob = CreateJobProgress(inJobId, JobStatusFlags.Default_Active);

            if (bCreated)
                Services.Events.Dispatch(GameEvents.JobStarted, inJobId);
            Services.Events.Dispatch(GameEvents.JobSwitched, inJobId);
            return true;
        }

        #endregion // Current Job

        #region Unlocking

        public bool UnlockHiddenJob(StringHash32 inJobId) {
            if (!Services.Assets.Jobs.IsHiddenJob(inJobId))
                return false;

            if (m_UnlockedJobs.Add(inJobId)) {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool IsHiddenUnlocked(StringHash32 inId) {
            return m_UnlockedJobs.Contains(inId);
        }

        #endregion // Unlocking

        #region Progress

        internal JobStatusFlags GetBaseStatus(StringHash32 inJobId) {
            Assert.True(Services.Assets.Jobs.HasId(inJobId), "Could not find JobDesc with id '{0}'", inJobId);

            if (m_InProgressJobs.Contains(inJobId)) {
                if (m_CurrentJobId == inJobId) {
                    return JobStatusFlags.Default_Active;
                } else {
                    return JobStatusFlags.Default_InProgress;
                }
            } else if (m_CompletedJobs.Contains(inJobId)) {
                return JobStatusFlags.Default_Completed;
            } else if (Services.Assets.Jobs.IsHiddenJob(inJobId) && !m_UnlockedJobs.Contains(inJobId)) {
                return JobStatusFlags.Hidden;
            } else {
                return JobStatusFlags.Mask_Available;
            }
        }

        public bool IsInProgress(StringHash32 inJobId) {
            Assert.True(Services.Assets.Jobs.HasId(inJobId), "Could not find JobDesc with id '{0}'", inJobId);
            return m_InProgressJobs.Contains(inJobId);
        }

        public bool IsStartedOrComplete(StringHash32 inJobId) {
            Assert.True(Services.Assets.Jobs.HasId(inJobId), "Could not find JobDesc with id '{0}'", inJobId);
            return m_InProgressJobs.Contains(inJobId) || m_CompletedJobs.Contains(inJobId);
        }

        public IReadOnlyCollection<StringHash32> InProgressJobIds() {
            return m_InProgressJobs;
        }

        public IReadOnlyCollection<StringHash32> CompletedJobIds() {
            return m_CompletedJobs;
        }

        /// <summary>
        /// Forgets that the given job was ever started.
        /// </summary>
        public void ForgetJob(StringHash32 inJobId) {
            m_CompletedJobs.Remove(inJobId);
            m_InProgressJobs.Remove(inJobId);
            if (inJobId == m_CurrentJobId) {
                SetCurrentJob(null);
            }
        }

        static private PlayerJob CreateJobProgress(StringHash32 inJobId, JobStatusFlags inStatus) {
            return new PlayerJob(inJobId, inStatus, Assets.Job(inJobId));
        }

        #endregion // Progress

        #region Completion

        public bool MarkComplete(StringHash32 inJobId) {
            Assert.False(inJobId.IsEmpty);
            Assert.True(Services.Assets.Jobs.HasId(inJobId), "Could not find JobDesc with id '{0}'", inJobId);

            if (m_CompletedJobs.Add(inJobId)) {
                bool bIsCurrent = m_CurrentJobId == inJobId;

                m_HasChanges = true;
                m_InProgressJobs.Remove(inJobId);
                m_CompletedTasks.RemoveWhere((t) => t.JobId == inJobId);

                Services.Events.FlushQueue();
                Services.Events.Dispatch(GameEvents.JobPreComplete, inJobId);
                Services.Events.Dispatch(GameEvents.JobCompleted, inJobId);

                if (bIsCurrent) {
                    Services.Events.Dispatch(GameEvents.JobUnload, m_CurrentJobId);

                    m_CurrentJobId = StringHash32.Null;
                    m_CurrentJob = default;
                    m_CurrentJobTaskIds.Clear();

                    Services.Events.Dispatch(GameEvents.JobPreload, StringHash32.Null);
                    Services.Events.Dispatch(GameEvents.JobSwitched, StringHash32.Null);
                }
                return true;
            }

            return false;
        }

        public bool IsComplete(StringHash32 inJobId) {
            Assert.True(Services.Assets.Jobs.HasId(inJobId), "Could not find JobDesc with id '{0}'", inJobId);
            return m_CompletedJobs.Contains(inJobId);
        }

        #endregion // Completion

        #region Tasks

        /// <summary>
        /// Returns if a given task is active for the current job.
        /// If no job is selected, then this will just return false.
        /// </summary>
        public bool IsTaskActive(StringHash32 inTaskId) {
            if (m_CurrentJobId.IsEmpty)
                return false;

            Assert.False(inTaskId.IsEmpty, "Cannot check null task");
            return m_CurrentJobTaskIds.Contains(inTaskId);
        }

        /// <summary>
        /// Returns if a given task is complete for the current job.
        /// If no job is selected, then this will just return false.
        /// </summary>
        public bool IsTaskComplete(StringHash32 inTaskId) {
            if (m_CurrentJobId.IsEmpty)
                return false;

            Assert.False(inTaskId.IsEmpty, "Cannot check null task");
            return m_CompletedTasks.Contains(new JobTaskKey(m_CurrentJobId, inTaskId));
        }

        /// <summary>
        /// Returns if a given task is at the top of the priority list.
        /// If no job is selected, then this will just return false.
        /// </summary>
        public bool IsTaskTop(StringHash32 inTaskId) {
            if (m_CurrentJobId.IsEmpty)
                return false;

            Assert.False(inTaskId.IsEmpty, "Cannot check null task");
            var taskList = m_CurrentJob.Job.Tasks();
            for (int i = 0; i < taskList.Length; i++) {
                StringHash32 taskId = taskList[i].Id;
                if (m_CurrentJobTaskIds.Contains(taskId))
                    return taskId == inTaskId;
            }
            return false;
        }

        /// <summary>
        /// Sets the given task as complete for the current job.
        /// </summary>
        public bool SetTaskComplete(StringHash32 inTaskId) {
            Assert.False(m_CurrentJobId.IsEmpty, "No current job to complete tasks for");
            Assert.False(inTaskId.IsEmpty, "Cannot complete null task");

            m_CurrentJobTaskIds.Remove(inTaskId);
            if (m_CompletedTasks.Add(new JobTaskKey(m_CurrentJobId, inTaskId))) {
                m_HasChanges = true;
                DebugService.Log(LogMask.DataService, "[JobsData] Task '{0}' on job '{1}' has been set completed", inTaskId, m_CurrentJobId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets a given task as active for the current job.
        /// </summary>
        public bool SetTaskActive(StringHash32 inTaskId) {
            Assert.False(m_CurrentJobId.IsEmpty, "No current job to modify tasks for");
            Assert.False(inTaskId.IsEmpty, "Cannot modify null task");

            m_CompletedTasks.Remove(new JobTaskKey(m_CurrentJobId, inTaskId));
            if (m_CurrentJobTaskIds.Add(inTaskId)) {
                DebugService.Log(LogMask.DataService, "[JobsData] Task '{0}' on job '{1}' has been set active", inTaskId, m_CurrentJobId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets a given task as inactive for the current job.
        /// </summary>
        public bool SetTaskInactive(StringHash32 inTaskId) {
            Assert.False(m_CurrentJobId.IsEmpty, "No current job to modify tasks for");
            Assert.False(inTaskId.IsEmpty, "Cannot modify null task");

            if (m_CompletedTasks.Remove(new JobTaskKey(m_CurrentJobId, inTaskId))) {
                m_HasChanges = true;
            }

            if (m_CurrentJobTaskIds.Remove(inTaskId)) {
                DebugService.Log(LogMask.DataService, "[JobsData] Task '{0}' on job '{1}' has been set inactive", inTaskId, m_CurrentJobId);
                return true;
            }

            return false;
        }

        #endregion // Tasks

        #region Debug

        #if DEVELOPMENT

        public void DebugMarkComplete(StringHash32 inJobId) {
            if (m_CompletedJobs.Add(inJobId)) {
                bool bIsCurrent = m_CurrentJobId == inJobId;

                m_HasChanges = true;
                m_InProgressJobs.Remove(inJobId);
                m_CompletedTasks.RemoveWhere((t) => t.JobId == inJobId);

                if (bIsCurrent) {
                    m_CurrentJobId = StringHash32.Null;
                    m_CurrentJob = default;
                    m_CurrentJobTaskIds.Clear();
                }
            }
        }

        #endif // DEVELOPMENT

        #endregion // Debug

        #region Clearing

        public void ClearAll() {
            SetCurrentJob(null);

            m_InProgressJobs.Clear();
            m_CompletedJobs.Clear();
            m_UnlockedJobs.Clear();
            m_CompletedTasks.Clear();

            Services.Events.Dispatch(GameEvents.ProfileRefresh);
        }

        #endregion // Clearing

        #region IProfileChunk

        // v2: switched job statuses to list of in-progress jobs
        ushort ISerializedVersion.Version { get { return 2; } }

        void ISerializedObject.Serialize(Serializer ioSerializer) {
            ioSerializer.UInt32Proxy("currentJobId", ref m_CurrentJobId);

            // old status list
            if (ioSerializer.ObjectVersion < 2) {
                List<PlayerJob> deprecatedJobs = new List<PlayerJob>();
                ioSerializer.ObjectArray("jobStatuses", ref deprecatedJobs);
                m_InProgressJobs.Clear();
                foreach (var job in deprecatedJobs)
                    m_InProgressJobs.Add(job.JobId);
            } else {
                ioSerializer.UInt32ProxySet("inProgressJobs", ref m_InProgressJobs);
            }

            ioSerializer.UInt32ProxySet("completedJobs", ref m_CompletedJobs);
            ioSerializer.UInt32ProxySet("unlockedJobs", ref m_UnlockedJobs);
            ioSerializer.ObjectSet("completedTasks", ref m_CompletedTasks);
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext) {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            SavePatcher.PatchId(ref m_CurrentJobId);
            SavePatcher.PatchIds(m_CompletedJobs);
            SavePatcher.PatchIds(m_InProgressJobs);
            SavePatcher.PatchIds(m_UnlockedJobs);

            var jobsDB = Services.Assets.Jobs;

            if (!m_CurrentJobId.IsEmpty) {
                if (!jobsDB.HasId(m_CurrentJobId)) {
                    Log.Warn("[JobsData] Unknown job id '{0}'", m_CurrentJobId);
                    m_CurrentJobId = StringHash32.Null;
                } else {
                    m_CurrentJob = CreateJobProgress(m_CurrentJobId, JobStatusFlags.Default_Active);
                    m_CurrentJobTaskIds.Clear();
                }
            }
        }

        public bool HasChanges() {
            return m_HasChanges;
        }

        public void MarkChangesPersisted() {
            m_HasChanges = false;
        }

        #endregion // IProfileChunk

        public void PostLoad() {
            Services.Events.Dispatch(GameEvents.JobPreload, m_CurrentJobId);

            if (!m_CurrentJobId.IsEmpty) {
                Services.Events.Dispatch(GameEvents.JobSwitched, m_CurrentJobId);
            }
        }
    }
}