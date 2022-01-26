using System;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Profile
{
    public class MapData : IProfileChunk, ISerializedVersion, ISerializedCallbacks
    {
        private enum TimeMode : byte
        {
            Normal,
            Realtime,
            
            FreezeAt0 = 16,
            FreezeAt2,
            FreezeAt4,
            FreezeAt6,
            FreezeAt8,
            FreezeAt10,
            FreezeAt12,
            FreezeAt14,
            FreezeAt16,
            FreezeAt18,
            FreezeAt20,
            FreezeAt22,
        }

        private StringHash32 m_CurrentStationId;
        private StringHash32 m_CurrentMapId;
        private StringHash32 m_CurrentMapEntranceId;

        private HashSet<StringHash32> m_UnlockedStationIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UnlockedSiteIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UnlockedRoomIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_VisitedLocations = new HashSet<StringHash32>();

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

        #region Unlocked Dive Sites

         public bool IsSiteUnlocked(StringHash32 inSiteId)
        {
            Assert.True(Services.Assets.Map.HasId(inSiteId), "Unknown site id '{0}'", inSiteId);
            return m_UnlockedSiteIds.Contains(inSiteId);
        }

        public bool UnlockSite(StringHash32 inSiteId)
        {
            Assert.True(Services.Assets.Map.HasId(inSiteId), "Unknown site id '{0}'", inSiteId);
            if (m_UnlockedSiteIds.Add(inSiteId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool LockSite(StringHash32 inSiteId)
        {
            Assert.True(Services.Assets.Map.HasId(inSiteId), "Unknown site id '{0}'", inSiteId);
            if (m_UnlockedSiteIds.Remove(inSiteId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        #endregion // Unlocked Dive Sites

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

        #region Seen

        public bool HasVisitedLocation(StringHash32 inLocationId)
        {
            return m_VisitedLocations.Contains(inLocationId);
        }

        public bool RecordVisitedLocation(StringHash32 inLocationId)
        {
            if (m_VisitedLocations.Add(inLocationId))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        #endregion // Seen

        public void SetDefaults()
        {
            m_CurrentStationId = Services.Assets.Map.DefaultStationId();
            m_UnlockedStationIds.Add(m_CurrentStationId);

            m_RandomSeedOffset = RNG.Instance.Next();

            foreach(var room in Services.Assets.Map.DefaultUnlockedRooms())
            {
                m_UnlockedRoomIds.Add(room);
            }

            foreach(var diveSite in Services.Assets.Map.DiveSites())
            {
                if (diveSite.HasFlags(MapFlags.UnlockedByDefault))
                    m_UnlockedSiteIds.Add(diveSite.Id());
            }
        }

        public void FullSync()
        {
            SyncMapId();
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

        public StringHash32 SavedSceneId() { return m_CurrentMapId; }
        public StringHash32 SavedSceneLocationId() { return m_CurrentMapEntranceId; }

        #region IProfileChunk

        // v5: site unlocks
        // v6: removed time :(
        // v7: added "seen"
        ushort ISerializedVersion.Version { get { return 7; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("stationId", ref m_CurrentStationId);
            ioSerializer.UInt32ProxySet("unlockedStations", ref m_UnlockedStationIds);
            ioSerializer.UInt32Proxy("currentMapId", ref m_CurrentMapId);

            if (ioSerializer.ObjectVersion >= 5)
            {
                ioSerializer.UInt32ProxySet("unlockedSites", ref m_UnlockedSiteIds);
            }
            else
            {
                foreach(var diveSite in Services.Assets.Map.DiveSites())
                {
                    if (diveSite.HasFlags(MapFlags.UnlockedByDefault))
                        m_UnlockedSiteIds.Add(diveSite.Id());
                }
            }

            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.UInt32Proxy("mapLocationId", ref m_CurrentMapEntranceId);
                if (ioSerializer.ObjectVersion < 6)
                {
                    Int64 temp0 = 0;
                    TimeMode mode = 0;
                    ioSerializer.Serialize("time", ref temp0);
                    ioSerializer.Enum("timeMode", ref mode);
                }
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

            if (ioSerializer.ObjectVersion >= 6)
            {
                ioSerializer.UInt32ProxySet("visitedLocations", ref m_VisitedLocations);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read)
                return;

            StringHash32 mapId = m_CurrentMapId;
            if (!Services.Assets.Map.HasId(mapId))
            {
                Log.Warn("[MapData] No map with id '{0}'", mapId);
                m_CurrentMapId = null;
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