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
        private List<ArgueData> m_ArgueData = new List<ArgueData>();
        private HashSet<StringHash32> m_CompletedArgues = new HashSet<StringHash32>();

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

        public SiteSurveyData TryGetSiteData(StringHash32 inMapId)
        {
            SiteSurveyData data;
            m_SiteData.TryGetValue<StringHash32, SiteSurveyData>(inMapId, out data);
            return data;
        }

        #endregion // Sites

        #region Argumentations

        public IReadOnlyList<ArgueData> ActiveArgues() { return m_ArgueData; }

        public ArgueData GetArgue(StringHash32 inArgumentId, out bool outbNew)
        {
            if (m_CompletedArgues.Contains(inArgumentId))
            {
                outbNew = false;
                return null;
            }

            ArgueData data;
            if (!m_ArgueData.TryGetValue<StringHash32, ArgueData>(inArgumentId, out data))
            {
                data = new ArgueData();
                data.Id = inArgumentId;
                data.OnChanged = MarkChanged;
                m_ArgueData.Add(data);
                m_HasChanges = true;
                outbNew = true;
            }
            else
            {
                outbNew = false;
            }

            return data;
        }

        public bool IsArgueCompleted(StringHash32 inArgumentId)
        {
            return m_CompletedArgues.Contains(inArgumentId);
        }

        public bool CompleteArgue(StringHash32 inArgumentId)
        {
            if (m_CompletedArgues.Add(inArgumentId))
            {
                ArgueData data;
                for(int i = 0; i < m_ArgueData.Count; i++)
                {
                    data = m_ArgueData[i];
                    if (data.Id == inArgumentId)
                    {
                        m_ArgueData.FastRemoveAt(i);
                        m_HasChanges = true;
                        break;
                    }
                }
                Services.Events.QueueForDispatch(GameEvents.ArgueDataUpdated, inArgumentId);
                return true;
            }
            
            return false;
        }

        #endregion // Argumentations

        #region IProfileChunk

        // v1: experiment data
        // v2: add site survey data
        // v3: remove experiment data
        // v4: add claim data
        ushort ISerializedVersion.Version { get { return 4; } }

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

            if (ioSerializer.ObjectVersion >= 4)
            {
                ioSerializer.ObjectArray("argues", ref m_ArgueData);
                ioSerializer.UInt32ProxySet("completedArgues", ref m_CompletedArgues);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            foreach(var data in m_SiteData)
            {
                data.OnChanged = MarkChanged;
            }

            foreach(var data in m_ArgueData)
            {
                data.OnChanged = MarkChanged;
            }
        }

        #endregion // IProfileChunk
    }
}