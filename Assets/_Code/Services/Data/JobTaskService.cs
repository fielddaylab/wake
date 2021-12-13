using System;
using BeauUtil;
using Aqua.Profile;
using BeauUtil.Services;
using BeauUtil.Debugger;
using System.Runtime.InteropServices;
using BeauRoutine;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(EventService))]
    internal partial class JobTaskService : ServiceBehaviour
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct TaskState
        {
            public JobTaskStatus Status;
            public TempList8<ushort> Prerequisites;
            public JobTask Task;
        }

        private enum TaskEventMask : uint
        {
            SceneLoad = 0x01,
            BestiaryUpdate = 0x02,
            StationUpdate = 0x04,
            ScriptNodeSeen = 0x08,
            VariableUpdated = 0x10,
            ObjectScanned = 0x20,
            InventoryUpdated = 0x40,
            SiteDataUpdated = 0x80,
            ArgumentUpdated = 0x100
        }

        [NonSerialized] private StringHash32 m_LoadedJobId;
        [NonSerialized] private TaskEventMask m_TaskMask;
        [NonSerialized] private bool m_JobLoading;
        private readonly RingBuffer<TaskState> m_TaskGraph = new RingBuffer<TaskState>(16, RingBufferMode.Expand);
        private readonly RingBuffer<ushort> m_TaskUpdateQueue = new RingBuffer<ushort>(16, RingBufferMode.Expand);

        protected override void Initialize()
        {
            base.Initialize();

            EventService events = Services.Events;
            events.Register<StringHash32>(GameEvents.JobPreload, OnJobPreload, this)
                .Register<StringHash32>(GameEvents.JobUnload, OnJobUnload, this)
                .Register(GameEvents.CutsceneEnd, OnCutsceneEnd, this)
                .Register(GameEvents.ProfileLoaded, OnProfileLoaded, this)
                .Register(GameEvents.PopupClosed, OnCutsceneEnd, this);

            RegisterDelayedTaskEvent(events, GameEvents.SceneLoaded, TaskEventMask.SceneLoad);
            RegisterTaskEvent(events, GameEvents.BestiaryUpdated, TaskEventMask.BestiaryUpdate);
            RegisterTaskEvent(events, GameEvents.StationChanged, TaskEventMask.StationUpdate);
            RegisterTaskEvent(events, GameEvents.ScriptNodeSeen, TaskEventMask.ScriptNodeSeen);
            RegisterDelayedTaskEvent(events, GameEvents.VariableSet, TaskEventMask.VariableUpdated);
            RegisterTaskEvent(events, GameEvents.ScanLogUpdated, TaskEventMask.ObjectScanned);
            RegisterTaskEvent(events, GameEvents.InventoryUpdated, TaskEventMask.InventoryUpdated);
            RegisterTaskEvent(events, GameEvents.SiteDataUpdated, TaskEventMask.SiteDataUpdated);
            RegisterTaskEvent(events, GameEvents.ArgueDataUpdated, TaskEventMask.ArgumentUpdated);
        }

        private void RegisterTaskEvent(EventService inService, StringHash32 inEventId, TaskEventMask inMask)
        {
            inService.Register(inEventId, () => ProcessUpdates(inMask), this);
        }

        private void RegisterDelayedTaskEvent(EventService inService, StringHash32 inEventId, TaskEventMask inMask)
        {
            inService.Register(inEventId, () => {
                Async.InvokeAsync(() => ProcessUpdates(inMask));
            }, this);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }

        #region Handlers

        private void OnProfileLoaded()
        {
            m_LoadedJobId = null;
            m_TaskGraph.Clear();
            m_TaskMask = 0;
            m_TaskUpdateQueue.Clear();
        }

        private void OnJobPreload(StringHash32 inJobId)
        {
            if (m_LoadedJobId == inJobId)
                return;

            Assert.True(m_TaskUpdateQueue.Count == 0, "Cannot preload while job queue contains entries");

            m_LoadedJobId = inJobId;
            m_TaskGraph.Clear();
            m_TaskMask = 0;

            if (inJobId.IsEmpty)
                return;

            SaveData saveData = Save.Current;
            
            m_JobLoading = true;

            ConstructTaskGraph(inJobId);
            SyncTaskStateFromProfile(saveData.Jobs);
            ProcessUpdates(0);

            m_JobLoading = false;
        }

        private void OnJobUnload(StringHash32 inJobId)
        {
            if (m_LoadedJobId != inJobId)
                return;

            ProcessUpdateQueue(Save.Jobs);

            m_LoadedJobId = StringHash32.Null;
            m_TaskGraph.Clear();
            m_TaskMask = 0;
        }

        private void OnCutsceneEnd()
        {
            if (!Script.ShouldBlock())
            {
                ProcessUpdateQueue(Save.Jobs);
            }
        }

        #endregion // Handlers

        #region Tasks

        private void ConstructTaskGraph(StringHash32 inJobId)
        {
            JobDesc job = Assets.Job(inJobId);
            ListSlice<JobTask> taskList = job.Tasks();

            // no tasks? no problem
            if (taskList.Length == 0)
                return;

            TaskEventMask eventMask = 0;

            for(int i = 0; i < taskList.Length; i++)
            {
                JobTask task = taskList[i];
                TaskState state;
                state.Task = task;
                state.Status = JobTaskStatus.Inactive;
                state.Prerequisites = new TempList8<ushort>(task.PrerequisiteTaskIndices);

                eventMask |= RetrieveMask(task);

                m_TaskGraph.PushBack(state);
            }

            m_TaskMask = eventMask;
        }

        private void SyncTaskStateFromProfile(JobsData inData)
        {
            for(int i = 0, length = m_TaskGraph.Count; i < length ; i++)
            {
                ref TaskState taskState = ref m_TaskGraph[i];
                taskState.Status = inData.IsTaskComplete(taskState.Task.Id) ? JobTaskStatus.Complete : JobTaskStatus.Inactive;
            }
        }

        private void ProcessUpdates(TaskEventMask inMask)
        {
            if (inMask != 0 && (m_TaskMask & inMask) == 0)
                return;
            
            SaveData saveData = Save.Current;
            JobsData jobsData = saveData.Jobs;

            ScanForUpdates(saveData, m_TaskUpdateQueue);
            if (!Script.ShouldBlock())
            {
                ProcessUpdateQueue(jobsData);
            }
        }

        private void ScanForUpdates(SaveData inData, RingBuffer<ushort> outUpdated)
        {
            // this assumes all tasks are sorted from root to leaf,
            // such that prerequisite tasks are always evaluated before the task that requires them

            for(int taskIdx = 0, taskCount = m_TaskGraph.Count; taskIdx < taskCount; taskIdx++)
            {
                ref TaskState taskState = ref m_TaskGraph[taskIdx];
                JobTaskStatus desiredStatus = GetDesiredState(inData, ref taskState);
                if (desiredStatus != taskState.Status)
                {
                    taskState.Status = desiredStatus;
                    outUpdated.PushBack((ushort) taskIdx);
                }
            }
        }

        private JobTaskStatus GetDesiredState(SaveData inData, ref TaskState inState)
        {
            if (inState.Status == JobTaskStatus.Complete)
                return JobTaskStatus.Complete;

            for(int i = 0, length = inState.Prerequisites.Count; i < length; i++)
            {
                int prereqIndex = inState.Prerequisites[i];
                if (m_TaskGraph[prereqIndex].Status != JobTaskStatus.Complete)
                    return JobTaskStatus.Inactive;
            }

            if (EvaluateTask(inState.Task, inData))
                return JobTaskStatus.Complete;

            return JobTaskStatus.Active;
        }

        private void ProcessUpdateQueue(JobsData inData)
        {
            int count = m_TaskUpdateQueue.Count;
            if (count <= 0)
                return;

            Assert.True(count <= 64, "More than 64 tasks updated in one frame, how the...");
            ulong updateMask = 0; // 64-bit mask indicating which tasks in the queue actually updated in the save data
            
            ushort taskIndex;
            ulong taskMask;
            for(int i = 0; i < count; i++)
            {
                taskMask = (ulong) 1 << i;
                taskIndex = m_TaskUpdateQueue[i];

                ref TaskState state = ref m_TaskGraph[taskIndex];
                switch(state.Status)
                {
                    case JobTaskStatus.Active:
                        if (inData.SetTaskActive(state.Task.Id))
                            updateMask |= taskMask;
                        break;

                    case JobTaskStatus.Complete:
                        if (inData.SetTaskComplete(state.Task.Id))
                            updateMask |= taskMask;
                        break;

                    case JobTaskStatus.Inactive:
                        if (inData.SetTaskInactive(state.Task.Id))
                            updateMask |= taskMask;
                        break;
                }
            }

            if (!m_JobLoading && updateMask != 0)
            {
                for(int i = 0; i < count; i++)
                {
                    taskMask = (ulong) 1 << i;
                    if ((taskMask & updateMask) == 0)
                        continue;
                    
                    taskIndex = m_TaskUpdateQueue[i];
                    
                    ref TaskState state = ref m_TaskGraph[taskIndex];
                    switch(state.Status)
                    {
                        case JobTaskStatus.Active:
                            Services.Events.Dispatch(GameEvents.JobTaskAdded, state.Task.Id.Hash());
                            break;

                        case JobTaskStatus.Complete:
                            Services.Events.Dispatch(GameEvents.JobTaskCompleted, state.Task.Id.Hash());
                            break;

                        case JobTaskStatus.Inactive:
                            Services.Events.Dispatch(GameEvents.JobTaskRemoved, state.Task.Id.Hash());
                            break;
                    }
                }

                Services.Events.Dispatch(GameEvents.JobTasksUpdated);
            }

            m_TaskUpdateQueue.Clear();
        }

        #endregion // Queue

        #region Evaluation
        
        static private TaskEventMask RetrieveMask(JobTask inTask)
        {
            TaskEventMask mask = 0;
            for(int i = 0, length = inTask.Steps.Length; i < length; i++)
            {
                switch(inTask.Steps[i].Type)
                {
                    case JobStepType.AcquireBestiaryEntry:
                    case JobStepType.AcquireFact:
                    case JobStepType.UpgradeFact:
                        mask |= TaskEventMask.BestiaryUpdate;
                        break;

                    case JobStepType.GotoScene:
                        mask |= TaskEventMask.SceneLoad;
                        break;

                    case JobStepType.GotoStation:
                        mask |= TaskEventMask.StationUpdate;
                        break;

                    case JobStepType.SeeScriptNode:
                        mask |= TaskEventMask.ScriptNodeSeen;
                        break;

                    case JobStepType.EvaluateCondition:
                        mask |= TaskEventMask.VariableUpdated;
                        break;

                    case JobStepType.ScanObject:
                        mask |= TaskEventMask.ObjectScanned;
                        break;

                    case JobStepType.GetItem:
                        mask |= TaskEventMask.InventoryUpdated;
                        break;

                    case JobStepType.AddFactToModel:
                        mask |= TaskEventMask.SiteDataUpdated;
                        break;

                    case JobStepType.FinishArgumentation:
                        mask |= TaskEventMask.ArgumentUpdated;
                        break;
                }
            }

            return mask;
        }

        static private bool EvaluateTask(JobTask inTask, SaveData inData)
        {
            JobStep[] steps = inTask.Steps;
            
            for(int i = 0, length = steps.Length; i < length; i++)
            {
                if (!EvaluateStep(steps[i], inData))
                    return false;
            }

            return true;
        }

        static private bool EvaluateStep(in JobStep inStep, SaveData inData)
        {
            switch(inStep.Type)
            {
                case JobStepType.GetItem:
                    return inData.Inventory.ItemCount(inStep.Target) >= inStep.Amount;

                case JobStepType.AcquireBestiaryEntry:
                    return inData.Bestiary.HasEntity(inStep.Target);

                case JobStepType.AcquireFact:
                    return inData.Bestiary.HasFact(inStep.Target);

                case JobStepType.UpgradeFact:
                    return inData.Bestiary.IsFactFullyUpgraded(inStep.Target);

                case JobStepType.EvaluateCondition:
                    return Services.Data.CheckConditions(inStep.ConditionString);

                case JobStepType.GotoScene:
                    return SceneHelper.ActiveScene().Name == inStep.Target.Hash();

                case JobStepType.GotoStation:
                    return inData.Map.CurrentStationId() == inStep.Target;

                case JobStepType.ScanObject:
                    return inData.Inventory.WasScanned(inStep.Target);

                case JobStepType.SeeScriptNode:
                    return inData.Script.HasSeen(inStep.Target, Scripting.PersistenceLevel.Profile);

                case JobStepType.AddFactToModel:
                    // TODO: Implement
                    // return inData.Bestiary.IsFactGraphed(inStep.Target);
                    return false;

                case JobStepType.FinishArgumentation:
                    return inData.Science.IsArgueCompleted(inStep.Target);

                default:
                    return true;
            }
        }

        #endregion // Evaluation
    }
}