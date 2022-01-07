using BeauUtil;

namespace Aqua
{
    public struct NamedOption
    {
        public readonly StringHash32 Id;
        public readonly StringHash32 TextId;
        public readonly bool Enabled;

        public NamedOption(StringHash32 inId, bool inbEnabled = true)
            : this(inId, inId, inbEnabled)
        { }

        public NamedOption(StringHash32 inId, StringHash32 inTextId, bool inbEnabled = true)
        {
            Id = inId;
            TextId = inTextId;
            Enabled = inbEnabled;
        }
    }
}