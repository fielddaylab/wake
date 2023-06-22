using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;
using System.IO;
using Aqua.Journal;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    public partial class JobDesc : DBObject, IBaked, IEditorOnlyData
    {
        private const int MaxTasks = ushort.MaxValue;

        internal enum JobTaskCategory
        {
            Unknown,
            Travel,
            Scan_Count,
            Experiment,
            Model,
            Argue,
            Narrative
        }

        [Serializable]
        internal class EditorJobTask
        {
            public SerializedHash32 Id;
            public TextId LabelId;
            public JobTaskCategory Category;
            [SerializeField, Range(0, 3)] public int TaskComplexity;
            [SerializeField, Range(0, 3)] public int  ScaffoldingComplexity;
            
            public JobStep[] Steps = null;

            [Header("Flow Control")]
            public SerializedHash32[] PrerequisiteTaskIds = null;

            [NonSerialized] public int Depth = -1;
        }

        #if UNITY_EDITOR

        public string[] EditorTaskIds() {
            return ArrayUtils.MapFrom(m_Tasks, (t) => t.Id.ToDebugString());
        }

        public string[] EditorReqTaskIds(StringHash32 id) {
            foreach(var task in m_Tasks) {
                if (task.Id == id) {
                    // return array of prereq task IDs from the given task ID
                    return ArrayUtils.MapFrom(task.PrerequisiteTaskIds, (t) => t.ToDebugString());
                }
            }
            Log.Error("[JobDesc] Task '{0}' not found in job '{1}', name, id.ToDebugString()");
            return null;
        }

        internal JobTaskCategory EditorTaskCategory(StringHash32 id) {
            foreach(var task in m_Tasks) {
                if (task.Id == id) {
                    return task.Category;
                }
            }
            return JobTaskCategory.Unknown;
        }

        internal int EditorTaskComplexity(StringHash32 id) {
            foreach(var task in m_Tasks){
                if (task.Id == id) {
                    return task.TaskComplexity;
                }
            }
            return 0;
        }

        internal int EditorTaskScaffoldingComplexity(StringHash32 id) {
            foreach(var task in m_Tasks){
                if (task.Id == id) {
                    return task.ScaffoldingComplexity;
                }
            }
            return 0;
        }

        public JobStep[] EditorJobTaskSteps(StringHash32 id) {
            foreach(var task in m_Tasks) {
                if (task.Id == id) {
                    return task.Steps;
                }
            }
            return null;
        }

        int IBaked.Order { get { return 16; }}

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            if (m_Tasks.Length > MaxTasks)
            {
                Log.Error("[JobDesc] Job '{0}' has more than {1} tasks", name, MaxTasks);
                Array.Resize(ref m_Tasks, MaxTasks);
            }

            foreach(var editorTask in m_Tasks) {
                if (editorTask.Category == JobTaskCategory.Unknown || editorTask.Category > JobTaskCategory.Narrative) {
                    editorTask.Category = ApproximateTaskCategory(editorTask.Steps);
                }
            }

            m_OptimizedTaskList = GenerateOptimizedTasks(m_Tasks);

            Assert.NotNull(m_Scripting, "[JobDesc] Job '{0}' has no script assigned", name);

            foreach(var upgrade in m_PrereqUpgrades) {
                Assert.False(upgrade.IsEmpty, "[JobDesc] Job '{0}' has a null prerequisite upgrade", name);
            }

            ValidationUtils.EnsureUnique(ref m_PrerequisiteJobs);
            ValidationUtils.EnsureUnique(ref m_ExtraAssets);

            bool validated = true;
            if (!m_JournalId.IsEmpty) {
                if (!ValidationUtils.FindAsset<JournalDesc>(m_JournalId.ToDebugString())) {
                    Log.Error("[JobDesc] Job '{0}' refers to unknown journal entry '{1}'", name, m_JournalId.ToDebugString());
                    validated = false;
                }
            }

            validated &= ValidateTaskIds(this);
            if (!validated)
                throw new BakeException("Elements on job {0} were invalid", name);
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

            ValidationUtils.StripDebugInfo(ref m_NameId);
            ValidationUtils.StripDebugInfo(ref m_DescId);
            ValidationUtils.StripDebugInfo(ref m_DescShortId);

            foreach(var task in m_OptimizedTaskList) {
                ValidationUtils.StripDebugInfo(ref task.LabelId);
                ValidationUtils.StripDebugInfo(ref task.Id);

                for(int i = 0; i < task.Steps.Length; i++) {
                    ValidationUtils.StripDebugInfo(ref task.Steps[i].Target);
                }
            }
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

                string jobDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(inItem));

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
                                BFBase fact;
                                if (!(fact = ValidationUtils.FindAsset<BFBase>(step.Target))) {
                                    Log.Error("Task '{0}' on job '{1}' references bestiary fact '{2}' that cannot be found", task.Id.Source(), inItem.Id(), step.Target.Source());
                                    bFailed = true;
                                } else {
                                    if (fact.Type == BFTypeId.Model) {
                                        BFModel model = fact as BFModel;
                                        if (model.ModelType != BFModelType.Custom) {
                                            ScriptableObject modelScope = null;
                                            foreach(var asset in inItem.m_ExtraAssets) {
                                                if (asset.GetType().Name == "JobModelScope") {
                                                    modelScope = asset;
                                                    break;
                                                }
                                            }
                                            if (!modelScope) {
                                                Log.Error("Task '{0}' on job '{1}' requires fact '{2}' and JobModelScope, but no JobModelScope found", task.Id.Source(), inItem.Id(), fact.Id.ToDebugString());
                                                bFailed = true;
                                            } else {
                                                string scopeDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(modelScope));
                                                if (scopeDirectory != jobDirectory) {
                                                    Log.Error("Task '{0}' on job '{1}' requires fact '{2}' and JobModelScope, but JobModelScope '{3}' is in another folder", task.Id.Source(), inItem.Id(), fact.Id.ToDebugString(), modelScope.name);
                                                    bFailed = true;
                                                }
                                            }
                                        }
                                    }
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

        static internal JobTaskCategory ApproximateTaskCategory(JobStep[] steps) {
            JobTaskCategory fallback = JobTaskCategory.Unknown;

            foreach(var step in steps) {
                switch(step.Type) {
                    case JobStepType.GotoScene:
                    case JobStepType.GotoStation:
                        return JobTaskCategory.Travel;

                    case JobStepType.UpgradeFact:
                        return JobTaskCategory.Experiment;

                    case JobStepType.ScanObject:
                        return JobTaskCategory.Scan_Count;

                    case JobStepType.EvaluateCondition:
                    case JobStepType.SeeScriptNode:
                    case JobStepType.GetItem:
                        fallback = JobTaskCategory.Narrative;
                        break;

                    case JobStepType.FinishArgumentation:
                        return JobTaskCategory.Argue;

                    case JobStepType.AddFactToModel:
                        return JobTaskCategory.Model;

                    case JobStepType.AcquireBestiaryEntry: {
                        BestiaryDesc entry = Assets.Bestiary(step.Target);
                        if (entry != null) {
                            if (entry.Category() == BestiaryDescCategory.Critter) {
                                return JobTaskCategory.Scan_Count;
                            } else {
                                fallback = JobTaskCategory.Travel;
                            }
                        }
                        break;
                    }

                    case JobStepType.AcquireFact: {
                        BFBase fact = Assets.Fact(step.Target);
                        if (fact != null) {
                            switch(fact.Type) {
                                case BFTypeId.WaterProperty:
                                case BFTypeId.WaterPropertyHistory:
                                case BFTypeId.Population:
                                case BFTypeId.PopulationHistory:
                                    return JobTaskCategory.Scan_Count;

                                case BFTypeId.Model:
                                    return JobTaskCategory.Model;

                                case BFTypeId.State:
                                case BFTypeId.Consume:
                                case BFTypeId.Death:
                                case BFTypeId.Eat:
                                case BFTypeId.Grow:
                                case BFTypeId.Parasite:
                                case BFTypeId.Reproduce:
                                case BFTypeId.Produce:
                                    return JobTaskCategory.Experiment;
                            }
                        }
                        break;
                    }
                }
            }

            return fallback;
        }

        #endif // UNITY_EDITOR
    }
}