using BeauUtil;

namespace ProtoAqua
{
    public struct NamedOption
    {
        public readonly StringHash Id;
        public readonly string Text;

        public NamedOption(string inText)
            : this(inText, inText)
        { }

        public NamedOption(StringHash inId, string inText)
        {
            Id = inId;
            Text = inText;
        }
    }
}