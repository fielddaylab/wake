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

        [Multiline]
        public string Description;

        public string PartnerIntroQuote;
        public string PartnerHelpQuote;
        public string PartnerCompleteQuote;

        [AutoEnum] public ContentArea ContentAreas;
        public bool Qualitative;

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 4; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("lastUpdated", ref LastUpdated);
            ioSerializer.Serialize("databaseId", ref DatabaseId);

            ioSerializer.Serialize("name", ref Name, string.Empty, FieldOptions.Optional);
            ioSerializer.Serialize("author", ref Author, string.Empty, FieldOptions.Optional);
            ioSerializer.Serialize("description", ref Description, string.Empty, FieldOptions.Optional);

            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Enum("contentAreas", ref ContentAreas);
                if (ioSerializer.ObjectVersion < 4)
                {
                    int difficulty = 1;
                    ioSerializer.Serialize("difficulty", ref difficulty, 1);
                }
            }

            if (ioSerializer.ObjectVersion >= 3)
            {
                ioSerializer.Serialize("qualitative", ref Qualitative, false);
            }

            if (ioSerializer.ObjectVersion >= 4)
            {
                ioSerializer.Serialize("partnerIntroQuote", ref PartnerIntroQuote, string.Empty);
                ioSerializer.Serialize("partnerHelpQuote", ref PartnerHelpQuote, string.Empty);
                ioSerializer.Serialize("partnerCompleteQuote", ref PartnerCompleteQuote, string.Empty);
            }
        }

        #endregion // ISerializedObject
    }
}