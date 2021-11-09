using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Job Database", fileName = "JobDB")]
    public class JobDB : DBObjectCollection<JobDesc>
    {
        #region Inspector

        [Header("Colors")]

        [SerializeField] private ColorPalette4 m_ActiveInlinePalette = new ColorPalette4(Color.white, ColorBank.Navy);
        [SerializeField] private ColorPalette4 m_CompletedInlinePalette = new ColorPalette4(ColorBank.LightGray, ColorBank.SlateBlue);

        [SerializeField] private ColorPalette4 m_ActivePortablePalette = new ColorPalette4(Color.white, ColorBank.Navy);
        [SerializeField] private ColorPalette4 m_CompletedPortablePalette = new ColorPalette4(ColorBank.LightGray, ColorBank.SlateBlue);

        #endregion // Inspector

        public ColorPalette4 ActiveInlinePalette() { return m_ActiveInlinePalette; }
        public ColorPalette4 CompletedInlinePalette() { return m_CompletedInlinePalette; }

        public ColorPalette4 ActivePortablePalette() { return m_ActivePortablePalette; }
        public ColorPalette4 CompletedPortablePalette() { return m_CompletedPortablePalette; }

        private HashSet<StringHash32> m_HiddenJobs;

        public IEnumerable<JobDesc> UnstartedJobs()
        {
            EnsureCreated();

            var jobsData = Save.Jobs;
            var mapData = Save.Map;

            foreach(var job in Objects)
            {
                if (!job.IsAtStation(mapData))
                    continue;
                if (jobsData.IsStartedOrComplete(job.Id()))
                    continue;
                if (!job.ShouldBeAvailable(jobsData))
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

            var jobsData = Save.Jobs;
            var mapData = Save.Map;

            var job = Get(inId);
            return job != null && job.IsAtStation(mapData) && !jobsData.IsStartedOrComplete(inId) && job.ShouldBeAvailable(jobsData);
        }

        public IEnumerable<JobDesc> VisibleJobs()
        {
            EnsureCreated();

            var jobsData = Save.Jobs;
            var mapData = Save.Map;

            foreach(var job in Objects)
            {
                if (jobsData.IsStartedOrComplete(job.Id()) || (job.IsAtStation(mapData) && job.ShouldBeAvailable(jobsData)))
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

            #if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
            JobDesc.ValidateTaskIds(inItem);
            #endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(JobDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}