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
        private HashSet<StringHash32> m_HiddenJobs;

        public IEnumerable<JobDesc> UnstartedJobs()
        {
            EnsureCreated();

            var jobsData = Services.Data.Profile.Jobs;

            foreach(var job in Objects)
            {
                if (!job.IsAtStation())
                    continue;
                if (jobsData.IsStartedOrComplete(job.Id()))
                    continue;
                if (!job.ShouldBeAvailable())
                    continue;
                
                yield return job;
            }
        }

        public bool HasUnstartedJobs()
        {
            EnsureCreated();

            var unstarted = UnstartedJobs().GetEnumerator();;
            return unstarted.MoveNext();
        }
        
        public bool IsAvailableAndUnstarted(StringHash32 inId)
        {
            EnsureCreated();

            var jobsData = Services.Data.Profile.Jobs;
            var job = Get(inId);
            return job != null && job.IsAtStation() && !jobsData.IsStartedOrComplete(inId) && job.ShouldBeAvailable();
        }

        public IEnumerable<JobDesc> VisibleJobs()
        {
            EnsureCreated();

            var jobsData = Services.Data.Profile.Jobs;

            foreach(var job in Objects)
            {
                if (jobsData.IsStartedOrComplete(job.Id()) || (job.IsAtStation() && job.ShouldBeAvailable()))
                {
                    yield return job;
                }
            }
        }

        public bool IsHiddenJob(StringHash32 inId)
        {
            return m_HiddenJobs.Contains(inId);
        }

        protected override void PreLookupConstruct()
        {
            base.PreLookupConstruct();
            m_HiddenJobs = new HashSet<StringHash32>();
        }

        protected override void ConstructLookupForItem(JobDesc inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);

            if (inItem.HasFlags(JobDescFlags.Hidden))
                m_HiddenJobs.Add(inItem.Id());
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(JobDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}