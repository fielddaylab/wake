using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System;
using BeauPools;
using System.Collections;
using BeauRoutine;
using System.Collections.Generic;
using Aqua.Profile;
using Aqua.Portable;
using UnityEngine.EventSystems;
using BeauUtil.Variants;

namespace Aqua
{
    public class InlineJobTaskList : SharedPanel
    {
        static private readonly TableKeyPair Var_NewJobRequest = TableKeyPair.Parse("session:jobSwitchedShowOnNewScene");

        #region Types
        
        private struct TaskOperation
        {
            public readonly StringHash32 TaskId;
            public readonly TaskOperationType Operation;

            public TaskOperation(StringHash32 inTaskId, TaskOperationType inType)
            {
                TaskId = inTaskId;
                Operation = inType;
            }
        }

        private enum TaskOperationType : byte
        {
            SwitchedJob,
            Activate,
            Complete,
            Remove
        }

        #endregion // Types

        #region Inspector

        [Header("Task List")]

        [SerializeField] private JobTaskDisplay.Pool m_TaskDisplays = null;
        [SerializeField] private float m_Spacing = 8;
        
        [Header("Animations")]

        [SerializeField] private float m_SlideDistance = 50;
        [SerializeField] private float m_SlideTime = 0.25f;

        [SerializeField] private float m_TaskFlashTime = 0.2f;
        [SerializeField] private float m_TaskSlideTime = 0.1f;
        [SerializeField] private float m_TaskAnimSpacing = 0.02f;
        [SerializeField] private float m_TaskCollapseAnimSpacing = 0.02f;
        [SerializeField] private float m_LingerDuration = 3;

        #endregion // Inspector

        [NonSerialized] private JobDesc m_CurrentJob = null;
        [NonSerialized] private JobDesc m_LastAnimatingJob = null;
        private RingBuffer<StringHash32> m_CurrentActiveTasks = new RingBuffer<StringHash32>(4, RingBufferMode.Expand);
        private RingBuffer<JobTaskDisplay> m_AllocatedTasks = new RingBuffer<JobTaskDisplay>(4, RingBufferMode.Expand);
        private RingBuffer<TaskOperation> m_OperationQueue = new RingBuffer<TaskOperation>(16, RingBufferMode.Expand);

        private Routine m_ProcessOperationsJob;

        #region Events

        protected override void Awake()
        {
            base.Awake();

            Services.Events.Register(GameEvents.SceneLoaded, OnSceneLoaded, this)
                .Register(GameEvents.JobSwitched, OnJobSwitched, this)
                .Register<StringHash32>(GameEvents.JobTaskAdded, OnTaskActivate, this)
                .Register<StringHash32>(GameEvents.JobTaskCompleted, OnTaskCompleted, this)
                .Register<StringHash32>(GameEvents.JobTaskRemoved, OnTaskDeactivate, this)
                .Register(GameEvents.CutsceneStart, OnCutsceneStart, this)
                .Register(GameEvents.CutsceneEnd, OnCutsceneEnd, this)
                .Register(GameEvents.PopupOpened, OnCutsceneStart, this)
                .Register(GameEvents.PopupClosed, OnCutsceneEnd, this)
                .Register(GameEvents.PortableOpened, OnPortableOpened, this);

            m_TaskDisplays.Initialize(null, null, 0);
            m_TaskDisplays.Config.RegisterOnConstruct((p, o) => {
                o.Click.onClick.AddListener(OnTaskButtonClicked);
            });
        }

        protected override void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
            m_ProcessOperationsJob.Stop();

            base.OnDestroy();
        }

        #endregion // Events

        #region Handlers

        private void OnSceneLoaded()
        {
            bool bDisplayOnLoad = Services.Data.PopVariable(Var_NewJobRequest).AsBool();
            ReloadTasks(true, bDisplayOnLoad);
            TryStartProcessing();
        }

        private void OnJobSwitched()
        {
            ReloadTasks(true, true);
        }
        
        private void OnTaskButtonClicked(PointerEventData _)
        {
            PortableMenu.OpenApp(PortableAppId.Job);
            
            SyncActiveTasks();
            m_ProcessOperationsJob.Stop();
            m_OperationQueue.Clear();

            Hide();
        }

        private void OnTaskCompleted(StringHash32 inTaskId)
        {
            QueueOperation(inTaskId, TaskOperationType.Complete);
        }

        private void OnTaskActivate(StringHash32 inTaskId)
        {
            QueueOperation(inTaskId, TaskOperationType.Activate);
        }

        private void OnTaskDeactivate(StringHash32 inTaskId)
        {
            QueueOperation(inTaskId, TaskOperationType.Remove);
        }

        private void OnCutsceneStart()
        {
            m_ProcessOperationsJob.Stop();
            Hide();
        }

        private void OnCutsceneEnd()
        {
            if (!Services.Valid)
                return;
            
            TryStartProcessing();
        }

        private void OnPortableOpened()
        {
            SyncActiveTasks();
            m_ProcessOperationsJob.Stop();
            m_OperationQueue.Clear();

            Hide();
        }

        private void TryStartProcessing()
        {
            if (m_OperationQueue.Count <= 0)
                return;
            
            if (Script.ShouldBlock())
                return;

            if (!m_ProcessOperationsJob)
            {
                if (m_CurrentJob == null)
                {
                    m_OperationQueue.Clear();
                    return;
                }
                
                m_LastAnimatingJob = m_CurrentJob;
                m_ProcessOperationsJob = Routine.Start(this, ProcessOperations());
            }
        }

        #endregion // Handlers

        #region Panel Animation

        protected override IEnumerator TransitionToShow()
        {
            return Routine.Combine(
                DefaultFadeOn(Root, CanvasGroup, m_SlideTime, Curve.CubeOut),
                Root.AnchorPosTo(0, m_SlideTime, Axis.X).ForceOnCancel().Ease(Curve.CubeOut)
            );
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                DefaultFadeOff(Root, CanvasGroup, m_SlideTime, Curve.CubeIn),
                Root.AnchorPosTo(m_SlideDistance, m_SlideTime, Axis.X).ForceOnCancel().Ease(Curve.CubeIn)
            );

            Root.SetAnchorPos(-m_SlideDistance, Axis.X);
        }

        protected override void InstantTransitionToHide()
        {
            base.InstantTransitionToHide();
            Root.SetAnchorPos(-m_SlideDistance, Axis.X);
        }

        protected override void InstantTransitionToShow()
        {
            base.InstantTransitionToShow();
            Root.SetAnchorPos(0, Axis.X);
        }

        protected override void OnShow(bool inbInstant)
        {
            InstantDisplayTasks();
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            ClearDisplayTasks();
            m_ProcessOperationsJob.Stop();
        }

        #endregion // Animation
    
        #region Tasks

        private void QueueOperation(StringHash32 inId, TaskOperationType inOperationType)
        {
            m_OperationQueue.PushBack(new TaskOperation(inId, inOperationType));
            TryStartProcessing();
        }

        private JobTaskDisplay FindDisplay(StringHash32 inTaskId)
        {
            foreach(var taskDisplay in m_AllocatedTasks)
            {
                if (taskDisplay.Task.Id == inTaskId)
                    return taskDisplay;
            }

            return null;
        }

        private void ReloadTasks(bool inbForce, bool inbDisplay)
        {
            JobDesc currentJob = Save.CurrentJob.Job;
            if (!inbForce && m_CurrentJob == currentJob)
                return;

            m_CurrentJob = currentJob;
            if (currentJob == null)
            {
                if (m_OperationQueue.Count > 0)
                    return;

                ClearDisplayTasks();
                return;
            }

            InstantHide();

            SyncActiveTasks();

            m_LastAnimatingJob = m_CurrentJob;
            InstantDisplayTasks();

            if (inbDisplay)
                QueueOperation(null, TaskOperationType.SwitchedJob);
        }

        private void SyncActiveTasks()
        {
            if (m_CurrentJob == null)
                return;
            
            JobsData jobsData = Save.Jobs;
            m_CurrentActiveTasks.Clear();
            foreach(var task in m_CurrentJob.Tasks())
            {
                if (jobsData.IsTaskActive(task.Id))
                    m_CurrentActiveTasks.PushBack(task.Id);
            }
        }

        private void InstantDisplayTasks()
        {
            if (!IsShowing())
                return;

            m_TaskDisplays.Reset();
            m_AllocatedTasks.Clear();
            if (m_CurrentJob != null)
            {
                foreach(var taskId in m_CurrentActiveTasks)
                {
                    JobTask task = m_CurrentJob.Task(taskId);
                    AllocTaskDisplay(task, false);
                }
            }

            InstantOrderTaskList();
        }

        private void ClearDisplayTasks()
        {
            m_TaskDisplays.Reset();
            m_AllocatedTasks.Clear();
            m_LastAnimatingJob = null;
        }

        private JobTaskDisplay AllocTaskDisplay(JobTask inTask, bool inbAnimating)
        {
            JobTaskDisplay taskDisplay = m_TaskDisplays.Alloc();
            taskDisplay.Populate(inTask, false);
            taskDisplay.Group.alpha = inbAnimating ? 0 : 1;
            m_AllocatedTasks.PushBack(taskDisplay);

            ColorPalette4 palette = Services.Assets.Jobs.ActiveInlinePalette();
            taskDisplay.Label.Graphic.color = palette.Content;
            taskDisplay.Background.color = palette.Background;
            return taskDisplay;
        }

        private IEnumerator ProcessOperations()
        {
            Show();

            while(IsTransitioning())
                yield return null;

            JobTaskDisplay taskDisplay;
            JobTask task;
            TaskOperation operation;
            while(true)
            {
                while(m_OperationQueue.TryPopFront(out operation))
                {
                    yield return m_TaskAnimSpacing;

                    switch(operation.Operation)
                    {
                        case TaskOperationType.SwitchedJob:
                            {
                                yield return 2;
                                break;
                            }

                        case TaskOperationType.Activate:
                            {
                                task = m_LastAnimatingJob.Task(operation.TaskId);
                                m_CurrentActiveTasks.PushBack(operation.TaskId);
                                
                                taskDisplay = FindDisplay(operation.TaskId);
                                if (taskDisplay == null)
                                {
                                    taskDisplay = AllocTaskDisplay(task, true);
                                    GenerateOrderingInfo();

                                    yield return Routine.Combine(
                                        ReorderTaskList(),
                                        ShowTaskAnimation(taskDisplay, 0)
                                    );
                                }

                                break;
                            }

                        case TaskOperationType.Complete:
                            {
                                task = m_LastAnimatingJob.Task(operation.TaskId);
                                taskDisplay = FindDisplay(operation.TaskId);
                                m_CurrentActiveTasks.FastRemove(operation.TaskId);

                                if (taskDisplay == null)
                                {
                                    taskDisplay = AllocTaskDisplay(task, true);
                                    GenerateOrderingInfo();

                                    yield return Routine.Combine(
                                        ReorderTaskList(),
                                        ShowTaskAnimation(taskDisplay, 0)
                                    );
                                }

                                yield return CompleteTaskAnimation(taskDisplay, 0);

                                yield return 0.5f;

                                GenerateOrderingInfo();

                                yield return Routine.Combine(
                                    ReorderTaskList(),
                                    HideTaskAnimation(taskDisplay, 0)
                                );
                                break;
                            }

                        case TaskOperationType.Remove:
                            {
                                m_CurrentActiveTasks.FastRemove(operation.TaskId);

                                taskDisplay = FindDisplay(operation.TaskId);
                                if (taskDisplay != null)
                                {
                                    m_AllocatedTasks.FastRemove(taskDisplay);

                                    GenerateOrderingInfo();

                                    yield return Routine.Combine(
                                        HideTaskAnimation(taskDisplay, 0),
                                        ReorderTaskList()
                                    );
                                }
                                break;
                            }
                    }
                }

                float duration = m_LingerDuration;
                while(duration > 0 && m_OperationQueue.Count == 0)
                {
                    yield return null;
                    duration -= Routine.DeltaTime;
                }

                if (duration <= 0)
                    break;
            }

            Hide();
            m_LastAnimatingJob = null;
        }

        #endregion // Tasks

        #region Task Animation

        private IEnumerator CompleteTaskAnimation(JobTaskDisplay inDisplay, float inDelay)
        {
            if (inDelay > 0)
                yield return inDelay;

            inDisplay.Completed = true;
            inDisplay.Checkmark.enabled = true;
            inDisplay.Flash.enabled = true;
            inDisplay.Flash.SetAlpha(1);

            inDisplay.Root.SetAsLastSibling();
            Services.Audio.PostEvent("job.taskComplete");

            ColorPalette4 palette = Services.Assets.Jobs.CompletedInlinePalette();
            inDisplay.Label.Graphic.color = palette.Content;
            inDisplay.Background.color = palette.Background;

            yield return inDisplay.Flash.FadeTo(0, m_TaskFlashTime);
            inDisplay.Flash.enabled = false;
        }

        private IEnumerator ShowTaskAnimation(JobTaskDisplay inDisplay, float inDelay)
        {
            if (inDelay > 0)
                yield return inDelay;

            inDisplay.Root.SetAsLastSibling();
            Services.Audio.PostEvent("job.newTask");
            
            inDisplay.Root.SetAnchorPos(-m_SlideDistance, Axis.X);
            inDisplay.Group.alpha = 0;

            yield return Routine.Combine(
                inDisplay.Root.AnchorPosTo(0, m_TaskSlideTime, Axis.X).Ease(Curve.CubeOut),
                inDisplay.Group.FadeTo(1, m_TaskSlideTime).Ease(Curve.CubeOut)
            );
        }

        private IEnumerator HideTaskAnimation(JobTaskDisplay inDisplay, float inDelay)
        {
            if (inDelay > 0)
                yield return inDelay;

            inDisplay.Root.SetAsLastSibling();

            yield return Routine.Inline(Routine.Combine(
                inDisplay.Root.AnchorPosTo(m_SlideDistance, m_TaskSlideTime, Axis.X).Ease(Curve.CubeIn),
                inDisplay.Group.FadeTo(0, m_TaskSlideTime).Ease(Curve.CubeIn)
            ));

            m_TaskDisplays.Free(inDisplay);
            m_AllocatedTasks.FastRemove(inDisplay);
        }

        #endregion // Task Animation

        #region Reordering

        private void GenerateOrderingInfo()
        {
            m_AllocatedTasks.Sort(JobTaskSorter);

            if (!Root.gameObject.activeSelf)
            {
                Root.gameObject.SetActive(true);
                CanvasGroup.alpha = 0;
            }

            foreach(var taskDisplay in m_AllocatedTasks)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(taskDisplay.Root);
            }

            float yAccum = 0;
            foreach(var taskDisplay in m_AllocatedTasks)
            {
                taskDisplay.DesiredY = yAccum;
                if (taskDisplay.Group.alpha == 0)
                    taskDisplay.Root.SetAnchorPos(yAccum, Axis.Y);
                
                if (!taskDisplay.Completed)
                    yAccum -= taskDisplay.Root.sizeDelta.y + m_Spacing;
            }
        }

        private IEnumerator ReorderTaskList()
        {
            IEnumerator[] moveAnims = new IEnumerator[m_AllocatedTasks.Count];
            
            JobTaskDisplay taskDisplay;
            int toMove = 0;
            for(int i = 0; i < moveAnims.Length; i++)
            {
                taskDisplay = m_AllocatedTasks[i];
                if (!taskDisplay.Completed && taskDisplay.Root.anchoredPosition.y != taskDisplay.DesiredY)
                {
                    moveAnims[i] = taskDisplay.Root.AnchorPosTo(taskDisplay.DesiredY, m_TaskSlideTime, Axis.Y).Ease(Curve.Smooth).DelayBy(toMove * m_TaskCollapseAnimSpacing);
                    toMove++;
                }
            }

            if (toMove > 0)
                yield return Routine.Combine(moveAnims);
        }

        private void InstantOrderTaskList()
        {
            GenerateOrderingInfo();

            JobTaskDisplay taskDisplay;
            for(int i = 0; i < m_AllocatedTasks.Count; i++)
            {
                taskDisplay = m_AllocatedTasks[i];
                taskDisplay.Group.alpha = 1;
                taskDisplay.Root.SetAnchorPos(taskDisplay.DesiredY, Axis.Y);
            }
        }

        static private readonly Comparison<JobTaskDisplay> JobTaskSorter = (a, b) => {
            return a.Task.Index.CompareTo(b.Task.Index);
        };

        #endregion // Reordering
    
        static public void RequestDisplayOnSceneLoad()
        {
            Script.WriteVariable(Var_NewJobRequest, true);
        }
    }
}