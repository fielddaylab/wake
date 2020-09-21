using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Profile
{
    public class InventoryData : ISerializedObject, ISerializedVersion
    {
        private HashSet<StringHash> m_ScannerIds = new HashSet<StringHash>();

        #region Scanner

        public bool WasScanned(StringHash inId)
        {
            return m_ScannerIds.Contains(inId);
        }

        public bool RegisterScanned(StringHash inId)
        {
            return m_ScannerIds.Add(inId);
        }

        #endregion // Scanner

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // TODO: Implement
        }

        #endregion // ISerializedData
    }
}