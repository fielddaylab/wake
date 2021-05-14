using BeauData;

namespace Aqua.Profile
{
    public class CharacterProfile : IProfileChunk, ISerializedVersion
    {
        public string DisplayName;
        public Pronouns Pronouns;

        ushort ISerializedVersion.Version { get { return 1; } }

        public void MarkChangesPersisted()
        {
            
        }

        public bool HasChanges()
        {
            return false;
        }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("displayName", ref DisplayName);
            ioSerializer.Enum("pronouns", ref Pronouns);
        }
    }
}