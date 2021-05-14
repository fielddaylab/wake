using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Profile
{
    public class MapData : IProfileChunk, ISerializedVersion
    {
        private StringHash32 m_CurrentStationId;
        private StringHash32 m_CurrentSceneId;
        private HashSet<StringHash32> m_UnlockedStationIds = new HashSet<StringHash32>();

        private bool m_HasChanges;

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

        public void SetDefaults()
        {
            m_CurrentStationId = Services.Assets.Map.DefaultStationId();
            m_UnlockedStationIds.Add(m_CurrentStationId);
        }

        public void SyncSceneId()
        {
            m_CurrentSceneId = MapDB.LookupMap(SceneHelper.ActiveScene());
        }

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("stationId", ref m_CurrentStationId);
            ioSerializer.Set("unlockedStations", ref m_UnlockedStationIds);

            ioSerializer.Serialize("currentSceneId", ref m_CurrentSceneId);
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