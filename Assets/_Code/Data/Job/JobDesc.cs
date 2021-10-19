using System;
using System.Collections.Generic;
using Aqua.Profile;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Jobs/Job Description", fileName = "NewJobDesc")]
    public partial class JobDesc : DBObject
    {
        #region Inspector

        [SerializeField, AutoEnum] private JobCategory m_Category = JobCategory.MainStory;
        [SerializeField, AutoEnum] private JobDescFlags m_Flags = 0;

        [SerializeField] private TextId m_NameId = default;
        [SerializeField] private TextId m_PosterId = default;
        [SerializeField] private TextId m_DescId = default;
        [SerializeField] private TextId m_DescShortId = default;
        [SerializeField] private TextId m_DescCompletedId = default;

        [SerializeField, Range(0, 5)] private int m_ExperimentDifficulty = 0;
        [SerializeField, Range(0, 5)] private int m_ModelingDifficulty = 0;
        [SerializeField, Range(0, 5)] private int m_ArgumentationDifficulty = 0;

        [SerializeField] private JobDesc[] m_PrerequisiteJobs = null;
        [SerializeField] private string m_PrereqConditions = null;

        [SerializeField, MapId(MapCategory.Station)] private SerializedHash32 m_StationId = null;
        [SerializeField, MapId(MapCategory.DiveSite)] private SerializedHash32[] m_DiveSiteIds = null;

        [SerializeField] internal EditorJobTask[] m_Tasks = null;
        [SerializeField, HideInInspector] private JobTask[] m_OptimizedTaskList = null;

        [SerializeField] private int m_CashReward = 0;
        [SerializeField] private int m_GearReward = 0;
        [SerializeField, ItemId] private SerializedHash32[] m_AdditionalRewards = null;

        [SerializeField] private LeafAsset m_Scripting = null;
        [SerializeField] private ScriptableObject[] m_ExtraAssets = null;

        #endregion // Inspector

        public JobCategory Category() { return m_Category; }
        public JobDescFlags Flags() { return m_Flags; }

        public bool HasFlags(JobDescFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(JobDescFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public TextId NameId() { return m_NameId; }
        public TextId PosterId() { return m_PosterId; }
        public TextId DescId() { return m_DescId; }
        public TextId DescShortId() { return m_DescShortId.IsEmpty ? m_DescId : m_DescShortId; }
        public TextId DescCompletedId() { return m_DescCompletedId.IsEmpty ? m_DescId : m_DescCompletedId; }

        public int Difficulty(ScienceActivityType inType)
        {
            switch(inType)
            {
                case ScienceActivityType.Argumentation:
                    return m_ArgumentationDifficulty;
                case ScienceActivityType.Experimentation:
                    return m_ExperimentDifficulty;
                case ScienceActivityType.Modeling:
                    return m_ModelingDifficulty;
                default:
                    throw new ArgumentOutOfRangeException("inType");
            }
        }

        public bool ShouldBeAvailable(JobsData inData)
        {
            if (HasFlags(JobDescFlags.Hidden) && !inData.IsHiddenUnlocked(Id()))
                return false;

            foreach(var job in m_PrerequisiteJobs)
            {
                if (!inData.IsComplete(job.Id()))
                    return false;
            }

            if (!string.IsNullOrEmpty(m_PrereqConditions))
                return Services.Data.CheckConditions(m_PrereqConditions);

            return true;
        }

        public StringHash32 StationId() { return m_StationId; }

        public bool IsAtStation(MapData inMap)
        {
            if (!m_StationId.IsEmpty)
            {
                if (inMap.CurrentStationId() != m_StationId)
                    return false;
            }

            return true;
        }

        public bool UsesDiveSite(StringHash32 inDiveSiteId)
        {
            return Array.IndexOf(m_DiveSiteIds, inDiveSiteId) >= 0;
        }

        public ListSlice<JobTask> Tasks() { return m_OptimizedTaskList; }
        
        public JobTask Task(StringHash32 inId)
        {
            for(int i = 0, length = m_OptimizedTaskList.Length; i < length; i++)
            {
                if (m_OptimizedTaskList[i].Id == inId)
                    return m_OptimizedTaskList[i];
            }

            Assert.True(false, "[JobDesc] No task with id '{0}' on job '{1}'", inId, Id());
            return null;
        }

        public int CashReward() { return m_CashReward; }
        public int GearReward() { return m_GearReward; }
        public IEnumerable<StringHash32> ExtraRewards()
        {
            foreach(var reward in m_AdditionalRewards)
                yield return reward;
        }

        public LeafAsset Scripting() { return m_Scripting; }

        public IEnumerable<T> FindAssets<T>() where T : ScriptableObject
        {
            T casted;
            foreach(var asset in m_ExtraAssets)
            {
                if ((casted = asset as T) != null)
                    yield return casted;
            }
        }

        public T FindAsset<T>() where T : ScriptableObject
        {
            T casted;
            foreach(var asset in m_ExtraAssets)
            {
                if ((casted = asset as T) != null)
                    return casted;
            }

            return null;
        }

        public T FindAsset<T>(StringHash32 inId) where T : ScriptableObject
        {
            T casted;
            foreach(var asset in m_ExtraAssets)
            {
                if ((casted = asset as T) != null && inId == casted.name)
                    return casted;
            }

            return null;
        }
    }

    public enum JobCategory
    {
        MainStory,
        SideStory
    }

    [LabeledEnum]
    public enum ScienceActivityType : byte
    {
        Experimentation,
        Modeling,
        Argumentation,

        [Hidden]
        MAX
    }

    [Flags]
    public enum JobDescFlags : uint
    {
        [Hidden] None = 0x0,
        Hidden = 0x0001
    }
}