using System;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Jobs/Job Database", fileName = "JobDB")]
    public class JobDB : DBObjectCollection<JobDesc>
    {
        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(JobDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}