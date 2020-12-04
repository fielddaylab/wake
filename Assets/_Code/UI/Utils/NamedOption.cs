using BeauUtil;

namespace Aqua
{
    public struct NamedOption
    {
        public readonly StringHash32 Id;
        public readonly string Text;

        public NamedOption(string inText)
            : this(inText, inText)
        { }

        public NamedOption(StringHash32 inId, string inText)
        {
            Id = inId;
            Text = inText;
        }
    }
}