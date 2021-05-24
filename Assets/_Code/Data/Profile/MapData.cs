using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Profile
{
    public class MapData : IProfileChunk, ISerializedVersion
    {
        private StringHash32 m_CurrentStationId;
        private StringHash32 m_CurrentMapId;
        private HashSet<StringHash32> m_UnlockedStationIds = new HashSet<StringHash32>();

        private bool m_HasChanges;

        #region Current Station

        public bool SetCurrentStationId(StringHash32 inNewStationId)
        {
            if (m_CurrentStationId != inNewStationId)
            {
                m_CurrentStationId = inNewStationId;
                m_HasChanges = true;
                Services.Events.Dispatch(GameEvents.StationChanged, inNewStationId);
                return true;
            }

            return false;
        }

        public StringHash32 CurrentStationId()
        {
            return m_CurrentStationId;
        }

        #endregion // Current Station

        #region Unlocked Stations

        public bool IsStationUnlocked(StringHash32 inStationId)
        {
            Assert.True(Services.Assets.Map.HasId(inStationId), "Unknown station id '{0}'", inStationId);
            return m_UnlockedStationIds.Contains(inStationId);
        }

        public bool UnlockStation(StringHash32 inStationId)
        {
            Assert.True(Services.Assets.Map.HasId(inStationId), "Unknown station id '{0}'", inStationId);
            if (m_UnlockedStationIds.Add(inStationId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool LockStation(StringHash32 inStationId)
        {
            Assert.True(Services.Assets.Map.HasId(inStationId), "Unknown station id '{0}'", inStationId);
            if (m_UnlockedStationIds.Remove(inStationId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        #endregion // Unlocked Stations

        public void SetDefaults()
        {
            m_CurrentStationId = Services.Assets.Map.DefaultStationId();
            m_UnlockedStationIds.Add(m_CurrentStationId);
        }

        public bool SyncMapId()
        {
            StringHash32 mapId = MapDB.LookupMap(SceneHelper.ActiveScene());
            if (!mapId.IsEmpty && m_CurrentMapId != mapId)
            {
                m_CurrentMapId = mapId;
                m_HasChanges = true;
                DebugService.Log(LogMask.DataService, "[MapData] Current map id is '{0}'", m_CurrentMapId);
                return true;
            }

            return false;
        }

        public StringHash32 SavedSceneId() { return m_CurrentMapId; }

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("stationId", ref m_CurrentStationId);
            ioSerializer.UInt32ProxySet("unlockedStations", ref m_UnlockedStationIds);

            ioSerializer.UInt32Proxy("currentMapId", ref m_CurrentMapId);
        }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        #endregion // IProfileChunk
    }
}