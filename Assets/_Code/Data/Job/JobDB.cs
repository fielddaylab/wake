using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Jobs/Job Database", fileName = "JobDB")]
    public class JobDB : DBObjectCollection<JobDesc>
    {
        public IEnumerable<JobDesc> UnstartedJobs()
        {
            var jobsData = Services.Data.Profile.Jobs;

            foreach(var job in Objects)
            {
                if (!job.IsAtStation())
                    continue;
                if (jobsData.IsStarted(job.Id()))
                    continue;
                if (!job.ShouldBeAvailable())
                    continue;
                
                yield return job;
            }
        }

        public IEnumerable<JobDesc> VisibleJobs()
        {
            var jobsData = Services.Data.Profile.Jobs;

            foreach(var job in Objects)
            {
                if (jobsData.IsStarted(job.Id()) || (job.IsAtStation() && job.ShouldBeAvailable()))
                {
                    yield return job;
                }
            }
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(JobDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}