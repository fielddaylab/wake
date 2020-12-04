using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua.Profile
{
    public class InventoryData : ISerializedObject, ISerializedVersion
    {
        private HashSet<StringHash32> m_ScannerIds = new HashSet<StringHash32>();

        #region Scanner

        public bool WasScanned(StringHash32 inId)
        {
            return m_ScannerIds.Contains(inId);
        }

        public bool RegisterScanned(StringHash32 inId)
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