using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Profile
{
    public class MapData : ISerializedObject, ISerializedVersion
    {
        private StringHash32 m_CurrentStationId = "Station1";

        public bool SetCurrentStationId(StringHash32 inNewStationId)
        {
            if (m_CurrentStationId != inNewStationId)
            {
                m_CurrentStationId = inNewStationId;
                return true;
            }

            return false;
        }

        public StringHash32 CurrentStationId()
        {
            return m_CurrentStationId;
        }

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // ioSerializer.Serialize("stationId", ref m_CurrentStationId);
        }

        #endregion // ISerializedObject
    }
}