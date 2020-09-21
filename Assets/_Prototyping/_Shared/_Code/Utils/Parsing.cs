
using BeauUtil;

namespace ProtoAqua
{
    static public class Parsing
    {
        static public readonly char[] CommaChar = new char[] { ',' };
        static public StringSlice.ISplitter QuoteAwareArgSplitter = new StringUtils.ArgsList.Splitter(false);
    }
}