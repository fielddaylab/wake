using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;

namespace Aqua
{
    public partial class JobDesc : DBObject, IBaked, IEditorOnlyData
    {
        private const int MaxTasks = ushort.MaxValue;

        [Serializable]
        internal class EditorJobTask
        {
            public SerializedHash32 Id;
            public TextId LabelId;
            
            public JobStep[] Steps = null;

            [Header("Flow Control")]
            public SerializedHash32[] PrerequisiteTaskIds = null;

            [NonSerialized] public int Depth = -1;
        }

        #if UNITY_EDITOR

        public string[] EditorTaskIds() {
            return ArrayUtils.MapFrom(m_Tasks, (t) => t.Id.ToDebugString());
        }

        int IBaked.Order { get { return 16; }}

        bool IBaked.Bake(BakeFlags flags)
        {
            if (m_Tasks.Length > MaxTasks)
            {
                Log.Error("[JobDesc] Job '{0}' has more than {1} tasks", name, MaxTasks);
                Array.Resize(ref m_Tasks, MaxTasks);
            }

            m_OptimizedTaskList = GenerateOptimizedTasks(m_Tasks);

            Assert.NotNull(m_Scripting, "[JobDesc] Job '{0}' has no script assigned", name);

            foreach(var upgrade in m_PrereqUpgrades) {
                Assert.False(upgrade.IsEmpty, "[JobDesc] Job '{0}' has a null prerequisite upgrade", name);
            }

            ValidationUtils.EnsureUnique(ref m_PrerequisiteJobs);
            ValidationUtils.EnsureUnique(ref m_ExtraAssets);

            bool validated = ValidateTaskIds(this);
            if (!validated)
                throw new BakeException("Tasks on {0} were invalid", name);
            return true;
        }

        static private JobTask[] GenerateOptimizedTasks(EditorJobTask[] inTasks)
        {
            // generate temporary data

            HashSet<StringHash32> endpointNodes = new HashSet<StringHash32>();
            foreach(var task in inTasks)
            {
                endpointNodes.Add(task.Id);
            }

            foreach(var task in inTasks)
            {
                task.Depth = -1;
                foreach(var prereq in task.PrerequisiteTaskIds)
                    endpointNodes.Remove(prereq);
            }

            // traverse

            foreach(var endpoint in endpointNodes)
            {
                int index = IndexOfTask(inTasks, endpoint);
                TraverseEditorTasks(inTasks, index, 0);
            }

            // sort by depth

            inTasks = (EditorJobTask[]) inTasks.Clone();
            Array.Sort(inTasks, CompareEditorTasks);

            // generate optimized data

            JobTask[] optimized = new JobTask[inTasks.Length];
            for(int taskIndex = 0; taskIndex < inTasks.Length; ++taskIndex)
            {
                EditorJobTask editorTask = inTasks[taskIndex];

                JobTask task = new JobTask();
                task.Id = editorTask.Id;
                task.IdString = editorTask.Id.Source();
                task.Index = (ushort) taskIndex;
                task.LabelId = editorTask.LabelId;
                task.Steps = (JobStep[]) editorTask.Steps.Clone();

                task.PrerequisiteTaskIndices = new ushort[editorTask.PrerequisiteTaskIds.Length];
                for(int i = 0; i < task.PrerequisiteTaskIndices.Length; i++)
                {
                    task.PrerequisiteTaskIndices[i] = IndexOfTask(inTasks, editorTask.PrerequisiteTaskIds[i]);
                }

                optimized[taskIndex] = task;
            }

            return optimized;
        }

        static private void TraverseEditorTasks(EditorJobTask[] inJobs, int inIndex, int inDepth)
        {
            ref EditorJobTask data = ref inJobs[inIndex];
            if (data.Depth == -1)
            {
                data.Depth = inDepth;
            }
            else
            {
                if (inDepth <= data.Depth)
                    return;
                
                data.Depth = inDepth;
            }

            foreach(var prereq in data.PrerequisiteTaskIds)
            {
                int prereqIndex = IndexOfTask(inJobs, prereq);
                if (prereqIndex != ushort.MaxValue && prereqIndex != inIndex)
                    TraverseEditorTasks(inJobs, prereqIndex, inDepth + 1);
            }
        }

        static private ushort IndexOfTask(EditorJobTask[] inJobs, StringHash32 inId)
        {
            for(int i = 0, length = inJobs.Length; i < length; i++)
            {
                if (inJobs[i].Id == inId)
                    return (ushort) i;
            }

            return ushort.MaxValue;
        }

        // deepest nodes are starting nodes
        static private Comparison<EditorJobTask> CompareEditorTasks = (a, b) => {
            return b.Depth.CompareTo(a.Depth);
        };

        void IEditorOnlyData.ClearEditorOnlyData()
        {
            m_Tasks = null;
        }

        static internal bool ValidateTaskIds(JobDesc inItem)
        {
            if (inItem.m_OptimizedTaskList.Length == 0)
                return true;

            bool bFailed = false;

            using(PooledSet<StringHash32> taskIds = PooledSet<StringHash32>.Create())
            {
                int rootCount = 0;
                foreach(var task in inItem.m_OptimizedTaskList)
                {
                    if (task.PrerequisiteTaskIndices.Length == 0)
                        rootCount++;
                    
                    if (!taskIds.Add(task.Id))
                    {
                        bFailed = true;
                        Log.Error("Duplicate task id '{0}' on job '{1}'", task.Id.Source(), inItem.Id());
                    }
                }

                if (rootCount == 0)
                {
                    bFailed = true;
                    Log.Error("No root tasks (tasks with 0 prerequisites) found for job '{0}'", inItem.Id());
                }

                foreach(var task in inItem.m_OptimizedTaskList)
                {
                    foreach(var prereq in task.PrerequisiteTaskIndices)
                    {
                        if (prereq == ushort.MaxValue)
                        {
                            bFailed = true;
                            Log.Error("Task '{0}' on job '{1}' has reference to unknown task", task.Id, inItem.Id());
                        }
                    }

                    foreach(var step in task.Steps)
                    {
                        switch(step.Type)
                        {
                            case JobStepType.AcquireBestiaryEntry: {
                                if (!ValidationUtils.FindAsset<BestiaryDesc>(step.Target)) {
                                    Log.Error("Task '{0}' on job '{1}' references bestiary entry '{2}' that cannot be found", task.Id.Source(), inItem.Id(), step.Target.Source());
                                    bFailed = true;
                                }
                                break;
                            }

                            case JobStepType.AcquireFact:
                            case JobStepType.AddFactToModel:
                            case JobStepType.UpgradeFact: {
                                if (!ValidationUtils.FindAsset<BFBase>(step.Target)) {
                                    Log.Error("Task '{0}' on job '{1}' references bestiary fact '{2}' that cannot be found", task.Id.Source(), inItem.Id(), step.Target.Source());
                                    bFailed = true;
                                }
                                break;
                            }

                            case JobStepType.GetItem: {
                                if (!ValidationUtils.FindAsset<InvItem>(step.Target)) {
                                    Log.Error("Task '{0}' on job '{1}' references item '{2}' that cannot be found", task.Id.Source(), inItem.Id(), step.Target.Source());
                                    bFailed = true;
                                }
                                break;
                            }

                            case JobStepType.GotoScene: {
                                if (!ValidationUtils.FindScene(step.Target.ToDebugString())) {
                                    Log.Error("Task '{0}' on job '{1}' references scene '{2}' that cannot be found", task.Id.Source(), inItem.Id(), step.Target.Source());
                                    bFailed = true;
                                }
                                break;
                            }

                            case JobStepType.GotoStation: {
                                if (!ValidationUtils.FindAsset<MapDesc>(step.Target)) {
                                    Log.Error("Task '{0}' on job '{1}' references station '{2}' that cannot be found", task.Id.Source(), inItem.Id(), step.Target.Source());
                                    bFailed = true;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            return !bFailed;
        }

        #endif // UNITY_EDITOR
    }
}