using BeauUtil;

namespace Aqua
{
    public struct NamedOption
    {
        public readonly StringHash32 Id;
        public readonly StringHash32 TextId;

        public NamedOption(StringHash32 inId)
            : this(inId, inId)
        { }

        public NamedOption(StringHash32 inId, StringHash32 inTextId)
        {
            Id = inId;
            TextId = inTextId;
        }
    }
}