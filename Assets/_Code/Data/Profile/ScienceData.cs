using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile
{
    public class ScienceData : IProfileChunk, ISerializedVersion
    {
        private List<InProgressExperimentData> m_CurrentExperiments = new List<InProgressExperimentData>();
        private List<SiteSurveyData> m_SiteData = new List<SiteSurveyData>();
        private HashSet<StringHash32> m_DirtyTanks = new HashSet<StringHash32>();

        private bool m_HasChanges;

        #region Experiments

        public IReadOnlyList<InProgressExperimentData> Experiments() { return m_CurrentExperiments; }
        
        public void AddExperiment(InProgressExperimentData inExperimentData)
        {
            Assert.True(!m_CurrentExperiments.Contains(inExperimentData), "Experiment data is already added");
            m_CurrentExperiments.Add(inExperimentData);
            m_HasChanges = true;
        }

        public InProgressExperimentData GetExperiment(StringHash32 inTankId)
        {
            InProgressExperimentData data;
            m_CurrentExperiments.TryGetValue<StringHash32, InProgressExperimentData>(inTankId, out data);
            return data;
        }

        public void RemoveExperiment(InProgressExperimentData inExperimentData)
        {
            Assert.True(m_CurrentExperiments.Contains(inExperimentData), "Experiment data is already removed");
            m_CurrentExperiments.FastRemove(inExperimentData);
            m_HasChanges = true;
        }

        #endregion // Experiments

        #region Tanks

        public bool IsTankDirty(StringHash32 inTankId)
        {
            return m_DirtyTanks.Contains(inTankId);
        }

        public bool SetTankDirty(StringHash32 inTankId)
        {
            if (m_DirtyTanks.Add(inTankId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool SetTankClean(StringHash32 inTankId)
        {
            if (m_DirtyTanks.Remove(inTankId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        #endregion // Tanks

        #region Sites

        public IReadOnlyList<SiteSurveyData> Sites() { return m_SiteData; }

        public SiteSurveyData GetSiteData(StringHash32 inMapId)
        {
            SiteSurveyData data;
            if (!m_SiteData.TryGetValue<StringHash32, SiteSurveyData>(inMapId, out data))
            {
                data = new SiteSurveyData();
                data.MapId = inMapId;
                m_SiteData.Add(data);
                m_HasChanges = true;
            }
            return data;
        }

        public void ClearSite(StringHash32 inMapId)
        {
            SiteSurveyData data;
            if (m_SiteData.TryGetValue<StringHash32, SiteSurveyData>(inMapId, out data))
            {
                data.TaggedCritters.Clear();
                m_HasChanges = true;
            }
        }

        #endregion // Sites

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 2; } }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("ongoingExperiments", ref m_CurrentExperiments);
            ioSerializer.UInt32ProxySet("dirtyTanks", ref m_DirtyTanks);
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.ObjectArray("siteSurveys", ref m_SiteData);
            }
        }

        #endregion // IProfileChunk
    }
}