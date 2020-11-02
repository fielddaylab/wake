
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;

namespace ProtoAqua
{
    static public class Parsing
    {
        static public readonly char[] CommaChar = new char[] { ',' };
        static public StringSlice.ISplitter QuoteAwareArgSplitter = new StringUtils.ArgsList.Splitter(false);

        static public readonly IBlockParsingRules Block = BlockParsingRules.Default;
        static public readonly IDelimiterRules InlineEvent = TagStringParser.CurlyBraceDelimiters;

        static public readonly string[] ReplaceTags = new string[] {
            "n", "newline", "highlight", "player-name", "cash", "gears", "loc",
            "pg", "var", "var-i", "var-f", "var-b", "var-s", "switch-var", "slow", "reallySlow"
        };
    }
}