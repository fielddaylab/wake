using System;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using EasyBugReporter;
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

        private HashSet<StringHash32> m_UnlockedStationIds = Collections.NewSet<StringHash32>(5);
        private HashSet<StringHash32> m_UnlockedSiteIds = Collections.NewSet<StringHash32>(18);
        private HashSet<StringHash32> m_UnlockedRoomIds = Collections.NewSet<StringHash32>(8);
        private HashSet<StringHash32> m_VisitedLocations = Collections.NewSet<StringHash32>(40);

        private int m_RandomSeedOffset;
        
        private bool m_HasChanges;

        #region Current Station

        public bool SetCurrentStationId(StringHash32 inNewStationId)
        {
            if (m_CurrentStationId != inNewStationId)
            {
                m_CurrentStationId = inNewStationId;
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.StationChanged, inNewStationId);
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
            Assert.True(Services.Assets.Map.HasId(inStationId), "Unknown station id '{0}'", inStationId.ToDebugString());
            return m_UnlockedStationIds.Contains(inStationId);
        }

        public bool UnlockStation(StringHash32 inStationId)
        {
            Assert.True(Services.Assets.Map.HasId(inStationId), "Unknown station id '{0}'", inStationId.ToDebugString());
            if (m_UnlockedStationIds.Add(inStationId))
            {
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.MapsUpdated);
                return true;
            }

            return false;
        }

        public bool LockStation(StringHash32 inStationId)
        {
            Assert.True(Services.Assets.Map.HasId(inStationId), "Unknown station id '{0}'", inStationId.ToDebugString());
            if (m_UnlockedStationIds.Remove(inStationId))
            {
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.MapsUpdated);
                return true;
            }

            return false;
        }

        #endregion // Unlocked Stations

        #region Unlocked Dive Sites

         public bool IsSiteUnlocked(StringHash32 inSiteId)
        {
            Assert.True(Services.Assets.Map.HasId(inSiteId), "Unknown site id '{0}'", inSiteId.ToDebugString());
            return m_UnlockedSiteIds.Contains(inSiteId);
        }

        public bool UnlockSite(StringHash32 inSiteId)
        {
            Assert.True(Services.Assets.Map.HasId(inSiteId), "Unknown site id '{0}'", inSiteId.ToDebugString());
            if (m_UnlockedSiteIds.Add(inSiteId))
            {
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.MapsUpdated);
                return true;
            }

            return false;
        }

        public bool LockSite(StringHash32 inSiteId)
        {
            Assert.True(Services.Assets.Map.HasId(inSiteId), "Unknown site id '{0}'", inSiteId.ToDebugString());
            if (m_UnlockedSiteIds.Remove(inSiteId))
            {
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.MapsUpdated);
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
                Services.Events.Queue(GameEvents.ViewLockChanged);
                return true;
            }

            return false;
        }

        public bool LockRoom(StringHash32 inRoomId)
        {
            if (m_UnlockedRoomIds.Remove(inRoomId))
            {
                m_HasChanges = true;
                Services.Events.Queue(GameEvents.ViewLockChanged);
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

        public IReadOnlyCollection<StringHash32> VisitedLocationIds()
        {
            return m_VisitedLocations;
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

        public void FullSync(StringHash32 inMapOverride = default(StringHash32))
        {
            SyncMapId(inMapOverride);
        }

        public bool SyncMapId(StringHash32 inMapOverride = default(StringHash32))
        {
            StringHash32 mapId = inMapOverride.IsEmpty ? MapDB.LookupCurrentMap() : inMapOverride;
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

            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            #endif // UNITY_EDITOR

            SavePatcher.PatchId(ref m_CurrentStationId);
            SavePatcher.PatchId(ref m_CurrentMapEntranceId);
            SavePatcher.PatchId(ref m_CurrentMapId);
            SavePatcher.PatchIds(m_UnlockedRoomIds);
            SavePatcher.PatchIds(m_UnlockedSiteIds);
            SavePatcher.PatchIds(m_UnlockedStationIds);
            SavePatcher.PatchIds(m_VisitedLocations);

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

        public void Dump(EasyBugReporter.IDumpWriter writer) {
            writer.Header("Location");
            writer.KeyValue("Current Map Id", Assets.NameOf(m_CurrentMapId));
            writer.KeyValue("Current Map Entrance", m_CurrentMapEntranceId.ToDebugString());
            writer.KeyValue("Current Station Id", Assets.NameOf(m_CurrentStationId));

            writer.Header("Unlocked Stations");
            foreach(var stationId in m_UnlockedStationIds) {
                writer.Text(Assets.NameOf(stationId));
            }

            writer.Header("Unlocked Sites");
            foreach(var siteId in m_UnlockedSiteIds) {
                writer.Text(Assets.NameOf(siteId));
            }

            writer.Header("Unlocked Rooms");
            foreach(var roomId in m_UnlockedRoomIds) {
                writer.Text(roomId.ToDebugString());
            }

            writer.Header("Visited Locations");
            foreach(var locationId in m_VisitedLocations) {
                writer.Text(locationId.ToDebugString());
            }
        }

        #endregion // IProfileChunk
    }
}