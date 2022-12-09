using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab System/Job Database", fileName = "JobDB")]
    public class JobDB : DBObjectCollection<JobDesc> {
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
        private Dictionary<StringHash32, List<JobDesc>> m_JobsPerSite;
        private List<JobDesc> m_CommonJobs;

        public ListSlice<JobDesc> JobsForStation(StringHash32 inStationId) {
            EnsureCreated();

            m_JobsPerSite.TryGetValue(inStationId, out List<JobDesc> jobs);
            return jobs;
        }

        public ListSlice<JobDesc> CommonJobs() {
            EnsureCreated();

            return m_CommonJobs;
        }

        public bool IsHiddenJob(StringHash32 inId) {
            EnsureCreated();
            
            return m_HiddenJobs.Contains(inId);
        }

        protected override void PreLookupConstruct() {
            base.PreLookupConstruct();
            m_HiddenJobs = new HashSet<StringHash32>();
            m_JobsPerSite = new Dictionary<StringHash32, List<JobDesc>>(5);
            m_CommonJobs = new List<JobDesc>();
        }

        protected override void ConstructLookupForItem(JobDesc inItem, int inIndex) {
            base.ConstructLookupForItem(inItem, inIndex);

            if (inItem.HasFlags(JobDescFlags.Hidden))
                m_HiddenJobs.Add(inItem.Id());

            List<JobDesc> bucket;
            StringHash32 stationId = inItem.StationId();
            if (stationId.IsEmpty) {
                bucket = m_CommonJobs;
            } else {
                if (!m_JobsPerSite.TryGetValue(stationId, out bucket)) {
                    bucket = new List<JobDesc>(16);
                    m_JobsPerSite.Add(stationId, bucket);
                }
            }

            bucket.Add(inItem);

            #if UNITY_EDITOR

            inItem.EditorInit();

            #endif // UNITY_EDITOR
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(JobDB))]
        private class Inspector : BaseInspector { }

        #endif // UNITY_EDITOR
    }
}