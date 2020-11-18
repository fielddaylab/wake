using BeauData;

namespace Aqua.Profile
{
    public class CharacterProfile : ISerializedObject, ISerializedVersion
    {
        public string DisplayName;
        public Pronouns Pronouns;

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("displayName", ref DisplayName);
            ioSerializer.Enum("pronouns", ref Pronouns);
        }
    }
}