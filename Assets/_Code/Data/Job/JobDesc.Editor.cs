using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    public partial class JobDesc : DBObject, IOptimizableAsset
    {
        #if UNITY_EDITOR

        // [CustomEditor(typeof(JobDesc)), CanEditMultipleObjects]
        private class Inspector : Editor
        {
            private SerializedProperty m_CategoryProperty;
            private SerializedProperty m_FlagsProperty;

            private SerializedProperty m_NameIdProperty;
            private SerializedProperty m_PosterIdProperty;
            private SerializedProperty m_DescIdProperty;
            private SerializedProperty m_DescShortIdProperty;
            private SerializedProperty m_DescCompletedIdProperty;

            private SerializedProperty m_ExperimentDifficultyProperty;
            private SerializedProperty m_ModelingDifficultyProperty;
            private SerializedProperty m_ArgumentationDifficultyProperty;

            private SerializedProperty m_PrerequisiteJobsProperty;
            private SerializedProperty m_PrereqConditionsProperty;

            private SerializedProperty m_StationIdProperty;
            private SerializedProperty m_DiveSiteIdsProperty;

            private SerializedProperty m_TasksProperty;

            private SerializedProperty m_CashRewardProperty;
            private SerializedProperty m_GearRewardProperty;
        }

        #endif // UNITY_EDITOR
    }
}