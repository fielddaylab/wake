
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using UnityEngine;

namespace Aqua
{
    static public class Parsing
    {
        static public readonly char[] CommaChar = new char[] { ',' };
        static public StringSlice.ISplitter QuoteAwareArgSplitter = new StringUtils.ArgsList.Splitter(false);

        static public readonly IBlockParsingRules Block = BlockParsingRules.Default;
        static public readonly IDelimiterRules InlineEvent = TagStringParser.CurlyBraceDelimiters;

        static public readonly string[] ReplaceTags = new string[] {
            "n", "newline", "highlight", "player-name", "cash", "gears", /*"loc",*/
            "pg", "var", "var-i", "var-f", "var-b", "var-s", "switch-var", "slow", "reallySlow"
        };

        static public Color ParseColor(StringSlice inString)
        {
            StringSlice color = inString;
            StringSlice alpha = StringSlice.Empty;

            int dotIdx = inString.IndexOf('.');
            if (dotIdx >= 0)
            {
                color = inString.Substring(0, dotIdx);
                alpha = inString.Substring(dotIdx + 1);
            }

            if (color.IsWhitespace)
                return Color.clear;

            Color parsedColor = Colors.HTML(color.ToString(), Color.white);
            if (!alpha.IsWhitespace)
                parsedColor.a *= StringParser.ParseInt(alpha, 100) / 100f;
            return parsedColor;
        }
    }
}