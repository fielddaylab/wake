using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [Serializable]
    public class ScenarioPackageHeader : ISerializedObject, ISerializedVersion
    {
        public string Id;
        public long LastUpdated;
        public string DatabaseId;

        public string Name;
        public string Author;
        public string Description;

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("lastUpdated", ref LastUpdated);
            ioSerializer.Serialize("databaseId", ref DatabaseId);

            ioSerializer.Serialize("name", ref Name, string.Empty, FieldOptions.Optional);
            ioSerializer.Serialize("author", ref Author, string.Empty, FieldOptions.Optional);
            ioSerializer.Serialize("description", ref Description, string.Empty, FieldOptions.Optional);
        }

        #endregion // ISerializedObject
    }
}