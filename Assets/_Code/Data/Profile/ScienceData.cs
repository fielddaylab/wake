using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile
{
    public class ScienceData : IProfileChunk, ISerializedVersion, ISerializedCallbacks
    {
        private List<SiteSurveyData> m_SiteData = new List<SiteSurveyData>();

        private bool m_HasChanges;

        #region Sites

        public IReadOnlyList<SiteSurveyData> Sites() { return m_SiteData; }

        public SiteSurveyData GetSiteData(StringHash32 inMapId)
        {
            SiteSurveyData data;
            if (!m_SiteData.TryGetValue<StringHash32, SiteSurveyData>(inMapId, out data))
            {
                data = new SiteSurveyData();
                data.MapId = inMapId;
                data.OnChanged = MarkChanged;
                m_SiteData.Add(data);
                m_HasChanges = true;
            }
            return data;
        }

        public void SetSiteVersion(StringHash32 inMapId, byte inVersion)
        {
            SiteSurveyData data = GetSiteData(inMapId);
            if (data.SiteVersion != inVersion)
            {
                data.TaggedCritters.Clear();
                data.SiteVersion = inVersion;
                m_HasChanges = true;
            }
        }

        #endregion // Sites

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 3; } }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        private void MarkChanged()
        {
            m_HasChanges = true;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            if (ioSerializer.ObjectVersion <= 2) {
                int[] _ = null;
                ioSerializer.Array("ongoingExperiments", ref _);

                HashSet<uint> __ = null;
                ioSerializer.Set("dirtyTanks", ref __);
            }
            
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.ObjectArray("siteSurveys", ref m_SiteData);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            foreach(var data in m_SiteData)
            {
                data.OnChanged = MarkChanged;
            }
        }

        #endregion // IProfileChunk
    }
}