
using BeauUtil;
using BeauUtil.Blocks;

namespace ProtoAqua
{
    static public class Parsing
    {
        static public readonly char[] CommaChar = new char[] { ',' };
        static public StringSlice.ISplitter QuoteAwareArgSplitter = new StringUtils.ArgsList.Splitter(false);

        static public readonly IBlockParsingRules Block = BlockParsingRules.Default;
    }
}