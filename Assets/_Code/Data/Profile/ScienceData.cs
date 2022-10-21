using System;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using EasyBugReporter;
using LogMask = Aqua.Debugging.LogMask;

namespace Aqua.Profile
{
    public class ScienceData : IProfileChunk, ISerializedVersion, ISerializedCallbacks
    {
        private List<SiteSurveyData> m_SiteData = new List<SiteSurveyData>();
        private List<ArgueData> m_ArgueData = new List<ArgueData>();
        private HashSet<StringHash32> m_CompletedArgues = new HashSet<StringHash32>();
        private uint m_CurrentLevel = 0;

        // specter/decryption stuff
        private uint m_SpecterCount = 0;
        private int m_SpecterQueued = 0;
        private StringHash32 m_SpecterSiteOverride = null;

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
                Services.Events.Queue(GameEvents.ArgueDataUpdated, inArgumentId);
                return true;
            }
            
            return false;
        }

        #endregion // Argumentations

        #region Leveling

        public uint CurrentLevel() { return m_CurrentLevel; }
        public bool SetCurrentLevel(uint inNextLevel)
        {
            if (m_CurrentLevel != inNextLevel)
            {
                DebugService.Log(LogMask.DataService, "[ScienceData] Player level changed from {0} to {1}", m_CurrentLevel, inNextLevel);

                Services.Events.Queue(GameEvents.ScienceLevelUpdated, new ScienceLevelUp() {
                    OriginalLevel = m_CurrentLevel,
                    LevelAdjustment = (int) inNextLevel - (int) m_CurrentLevel
                });

                m_CurrentLevel = inNextLevel;
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        #endregion // Leveling

        #region Specters

        public uint SpecterCount() { return m_SpecterCount; }
        public bool SetSpecterCount(uint inDecryptLevel)
        {
            if (m_SpecterCount != inDecryptLevel)
            {
                DebugService.Log(LogMask.DataService, "[ScienceData] Player decrypt level changed to {0}", inDecryptLevel);

                Services.Events.Queue(GameEvents.DecryptLevelUpdated, inDecryptLevel);
                m_SpecterCount = inDecryptLevel;
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool FullyDecrypted() {
            return m_SpecterCount >= ScienceUtils.MaxSpecters();
        }

        public bool IsSpecterQueued(StringHash32 mapId) {
            return m_SpecterQueued > 0 && (m_SpecterSiteOverride.IsEmpty || m_SpecterSiteOverride == mapId);
        }

        public void QueueSpecter(StringHash32 mapIdOverride = default(StringHash32)) {
            m_SpecterQueued++;
            m_SpecterSiteOverride = mapIdOverride;
            m_HasChanges = true;
        }

        public void DequeueSpecter() {
            if (m_SpecterQueued > 0) {
                m_SpecterQueued--;
                m_SpecterSiteOverride = null;
                m_HasChanges = true;
            }
        }

        #endregion // Specters

        #region IProfileChunk

        // v1: experiment data
        // v2: add site survey data
        // v3: remove experiment data
        // v4: add claim data
        // v5: added level
        // v6: added specter fields
        ushort ISerializedVersion.Version { get { return 6; } }

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
            if (ioSerializer.ObjectVersion < 3) {
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

            if (ioSerializer.ObjectVersion >= 5)
            {
                ioSerializer.Serialize("level", ref m_CurrentLevel);
            }

            if (ioSerializer.ObjectVersion >= 6)
            {
                ioSerializer.Serialize("decrypt", ref m_SpecterCount);
                ioSerializer.Serialize("spectersQueued", ref m_SpecterQueued);
                ioSerializer.UInt32Proxy("spectersQueueOverride", ref m_SpecterSiteOverride);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            #endif // UNITY_EDITOR

            foreach(var data in m_SiteData)
            {
                SavePatcher.PatchId(ref data.MapId);
                SavePatcher.PatchIds(data.TaggedCritters);
                SavePatcher.PatchIds(data.GraphedCritters);
                SavePatcher.PatchIds(data.GraphedFacts);
                data.OnChanged = MarkChanged;
            }

            foreach(var data in m_ArgueData)
            {
                SavePatcher.PatchIds(data.ExpectedFacts);
                SavePatcher.PatchIds(data.SubmittedFacts);
                data.OnChanged = MarkChanged;
            }
        }

        public void Dump(EasyBugReporter.IDumpWriter writer) {
            writer.KeyValue("Science Level", m_CurrentLevel);

            foreach(var siteSurvey in m_SiteData) {
                writer.Header("Survey Data for " + Assets.NameOf(siteSurvey.MapId));
                foreach(var taggedId in siteSurvey.TaggedCritters) {
                    writer.KeyValue("Tagged", Assets.NameOf(taggedId));
                }
                foreach(var graphedId in siteSurvey.GraphedCritters) {
                    writer.KeyValue("Graphed", Assets.NameOf(graphedId));
                }
                foreach(var graphedId in siteSurvey.GraphedFacts) {
                    writer.KeyValue("Graphed", Assets.NameOf(graphedId));
                }
            }

            writer.Header("Completed Argumentations");
            foreach(var argueId in m_CompletedArgues) {
                writer.Text(argueId.ToDebugString());
            }

            foreach(var inProgress in m_ArgueData) {
                writer.Header("Argument " + inProgress.Id.ToDebugString() + " in progress");
                writer.KeyValue("Claim Id", inProgress.ClaimId.ToDebugString());
            }
        }

        #endregion // IProfileChunk
    }
}