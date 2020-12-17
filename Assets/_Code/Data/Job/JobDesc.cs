using System;
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
        [SerializeField] private SerializedHash32 m_NameId;
        [SerializeField] private SerializedHash32 m_PosterId;
        [SerializeField] private SerializedHash32 m_DescId;
        [SerializeField] private SerializedHash32 m_DescInProgressId;
        [SerializeField] private SerializedHash32 m_DescCompletedId;

        [Header("Info")]
        [SerializeField, Range(0, 4)] private int m_ExperimentDifficulty = 0;
        [SerializeField, Range(0, 5)] private int m_ModelingDifficulty = 0;
        [SerializeField, Range(0, 5)] private int m_ArgumentationDifficulty = 0;

        [Header("Conditions")]
        [SerializeField] private JobDesc[] m_PrerequisiteJobs = null;
        [SerializeField] private string m_PrereqConditions = null;

        [Header("Rewards")]
        [SerializeField] private int m_CashReward = 0;
        [SerializeField] private int m_GearReward = 0;
        [SerializeField] private SerializedHash32[] m_AdditionalRewards = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private LeafAsset m_Scripts = null;

        #endregion // Inspector
    }

    public enum JobCategory
    {
        MainStory,
        SideStory
    }

    [Flags]
    public enum JobDescFlags : uint
    {
        [Hidden] None = 0x0,
        Hidden = 0x0001
    }
}