using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Jobs/Job Description", fileName = "NewJobDesc")]
    public class JobDesc : DBObject
    {
        #region Inspector

        [SerializeField, AutoEnum] private JobCategory m_Category = JobCategory.MainStory;
        [SerializeField, AutoEnum] private JobDescFlags m_Flags = 0;

        [Header("Text")]
        [SerializeField] private SerializedHash32 m_NameId = null;
        [SerializeField] private SerializedHash32 m_PosterId = null;
        [SerializeField] private SerializedHash32 m_DescId = null;
        [SerializeField] private SerializedHash32 m_DescInProgressId = null;
        [SerializeField] private SerializedHash32 m_DescCompletedId = null;

        [Header("Info")]
        [SerializeField, Range(0, 5)] private int m_ExperimentDifficulty = 0;
        [SerializeField, Range(0, 5)] private int m_ModelingDifficulty = 0;
        [SerializeField, Range(0, 5)] private int m_ArgumentationDifficulty = 0;

        [Header("Conditions")]
        [SerializeField] private JobDesc[] m_PrerequisiteJobs = null;
        [SerializeField] private string m_PrereqConditions = null;

        [Header("Locations")]
        [SerializeField] private string m_StationId = null;
        [SerializeField] private string[] m_DiveSiteIds = null;

        [Header("Rewards")]
        [SerializeField] private int m_CashReward = 0;
        [SerializeField] private int m_GearReward = 0;
        [SerializeField] private SerializedHash32[] m_AdditionalRewards = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private LeafAsset m_Scripting = null;
        [SerializeField] private ScriptableObject[] m_ExtraAssets = null;

        #endregion // Inspector

        public JobCategory Category() { return m_Category; }
        public JobDescFlags Flags() { return m_Flags; }

        public bool HasFlags(JobDescFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(JobDescFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public StringHash32 NameId() { return m_NameId; }
        public StringHash32 PosterId() { return m_PosterId; }
        public StringHash32 DescId() { return m_DescId; }
        public StringHash32 DescInProgressId() { return m_DescInProgressId; }
        public StringHash32 DescCompletedId() { return m_DescCompletedId; }

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

        public bool ShouldBeAvailable()
        {
            var jobs = Services.Data.Profile.Jobs;

            if (HasFlags(JobDescFlags.Hidden) && !jobs.IsHiddenUnlocked(Id()))
                return false;

            foreach(var job in m_PrerequisiteJobs)
            {
                if (!jobs.IsComplete(job.Id()))
                    return false;
            }

            if (!string.IsNullOrEmpty(m_PrereqConditions))
                return Services.Data.CheckConditions(m_PrereqConditions);

            return true;
        }

        public bool IsAtStation()
        {
            var map = Services.Data.Profile.Map;

            if (!string.IsNullOrEmpty(m_StationId))
            {
                if (map.CurrentStationId() != m_StationId)
                    return false;
            }

            return true;
        }

        public bool UsesDiveSite(string inDiveSiteId)
        {
            return Array.IndexOf(m_DiveSiteIds, inDiveSiteId) >= 0;
        }

        public int CashReward() { return m_CashReward; }
        public int GearReward() { return m_GearReward; }
        public IEnumerable<StringHash32> ExtraRewards()
        {
            foreach(var reward in m_AdditionalRewards)
                yield return reward;
        }

        public Sprite Icon() { return m_Icon; }
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