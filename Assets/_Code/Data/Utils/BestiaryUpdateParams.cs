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
            RemovedFact
        }

        public UpdateType Type;
        public StringHash32 Id;

        public BestiaryUpdateParams(UpdateType inType, StringHash32 inId)
        {
            Type = inType;
            Id = inId;
        }
    }
}