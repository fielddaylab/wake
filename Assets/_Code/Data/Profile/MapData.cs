using System;
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
        private StringHash32 m_CurrentMapEntranceId;

        private HashSet<StringHash32> m_UnlockedStationIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UnlockedSiteIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UnlockedRoomIds = new HashSet<StringHash32>();

        public GTDate CurrentTime;
        public TimeMode TimeMode = TimeMode.FreezeAt12;

        private int m_RandomSeedOffset;
        
        private bool m_HasChanges;

        #region Current Station

        public bool SetCurrentStationId(StringHash32 inNewStationId)
        {
            if (m_CurrentStationId != inNewStationId)
            {
                m_CurrentStationId = inNewStationId;
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.StationChanged, inNewStationId);
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

        #region Ship Rooms

        public bool IsRoomUnlocked(StringHash32 inRoomId)
        {
            return m_UnlockedRoomIds.Contains(inRoomId);
        }

        public bool UnlockRoom(StringHash32 inRoomId)
        {
            if (m_UnlockedRoomIds.Add(inRoomId))
            {
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.RoomLockChanged);
                return true;
            }

            return false;
        }

        public bool LockRoom(StringHash32 inRoomId)
        {
            if (m_UnlockedRoomIds.Remove(inRoomId))
            {
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.RoomLockChanged);
                return true;
            }

            return false;
        }

        #endregion // Ship Rooms

        public void SetDefaults()
        {
            m_CurrentStationId = Services.Assets.Map.DefaultStationId();
            m_UnlockedStationIds.Add(m_CurrentStationId);

            CurrentTime = Services.Time.StartingTime();
            m_RandomSeedOffset = RNG.Instance.Next();

            foreach(var room in Services.Assets.Map.DefaultUnlockedRooms())
            {
                m_UnlockedRoomIds.Add(room);
            }
        }

        public void FullSync()
        {
            SyncMapId();
            SyncTime();
        }

        public bool SyncMapId()
        {
            StringHash32 mapId = MapDB.LookupCurrentMap();
            if (!mapId.IsEmpty && m_CurrentMapId != mapId)
            {
                m_CurrentMapId = mapId;
                m_HasChanges = true;
                DebugService.Log(LogMask.DataService, "[MapData] Current map id is '{0}' with entrance '{1}'", m_CurrentMapId, m_CurrentMapEntranceId);
                return true;
            }

            return false;
        }

        public bool SetEntranceId(StringHash32 inEntranceId)
        {
            if (inEntranceId != m_CurrentMapEntranceId)
            {
                m_CurrentMapEntranceId = inEntranceId;
                m_HasChanges = true;
                DebugService.Log(LogMask.DataService, "[MapData] Current map entrance is '{0}'", m_CurrentMapEntranceId);
                return true;
            }

            return false;
        }

        public bool SyncTime()
        {
            Services.Time.ProcessQueuedChanges();

            GTDate currentTime = Services.Time.Current;
            if (currentTime != CurrentTime)
            {
                CurrentTime = currentTime;
                m_HasChanges = true;
                DebugService.Log(LogMask.DataService, "[MapData] Current time id is '{0}'", currentTime);
                return true;
            }

            return false;
        }

        public StringHash32 SavedSceneId() { return m_CurrentMapId; }
        public StringHash32 SavedSceneLocationId() { return m_CurrentMapEntranceId; }

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 4; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("stationId", ref m_CurrentStationId);
            ioSerializer.UInt32ProxySet("unlockedStations", ref m_UnlockedStationIds);
            ioSerializer.UInt32Proxy("currentMapId", ref m_CurrentMapId);

            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.UInt32Proxy("mapLocationId", ref m_CurrentMapEntranceId);
                ioSerializer.Int64Proxy("time", ref CurrentTime);
                ioSerializer.Enum("timeMode", ref TimeMode);
            }

            if (ioSerializer.ObjectVersion >= 3)
            {
                ioSerializer.Serialize("randomSeedOffset", ref m_RandomSeedOffset);
            }
            else
            {
                m_RandomSeedOffset = RNG.Instance.Next();
            }

            if (ioSerializer.ObjectVersion >= 4)
            {
                ioSerializer.UInt32ProxySet("unlockedRooms", ref m_UnlockedRoomIds);
            }
            else
            {
                foreach(var room in Services.Assets.Map.DefaultUnlockedRooms())
                {
                    m_UnlockedRoomIds.Add(room);
                }
            }
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