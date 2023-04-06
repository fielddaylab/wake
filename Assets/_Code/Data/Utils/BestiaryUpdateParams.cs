using BeauUtil;

namespace Aqua
{
    public struct BestiaryUpdateParams
    {
        public enum UpdateType : byte
        {
            Entity,
            Fact,
            UpgradeFact,

            RemovedEntity,
            RemovedFact,

            Unknown = 255
        }

        public StringHash32 Id;
        public UpdateType Type;

        public BestiaryUpdateParams(UpdateType inType, StringHash32 inId)
        {
            Type = inType;
            Id = inId;
        }
    }
}