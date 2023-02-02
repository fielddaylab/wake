
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
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
            "n", "newline", "highlight", "player-name", "cash", "exp", /*"loc",*/
            "pg", "var", "var-i", "var-f", "var-b", "var-s", "switch-var", "slow", "reallySlow"
        };

        static public readonly TagString WorkingTag = new TagString();

        static public Color HexColor(string inColorString)
        {
            Assert.True(inColorString.Length == 7 && inColorString[0] == '#');
            byte colorR = (byte) ((HexValue(inColorString[1]) << 4) + HexValue(inColorString[2]));
            byte colorG = (byte) ((HexValue(inColorString[3]) << 4) + HexValue(inColorString[4]));
            byte colorB = (byte) ((HexValue(inColorString[5]) << 4) + HexValue(inColorString[6]));
            return new Color(colorR / 255f, colorG / 255f, colorB / 255f);
        }

        [MethodImpl(256)]
        static private int HexValue(char inCharacter)
        {
            if (inCharacter >= 'a')
                return 10 + inCharacter - 'a';
            return inCharacter - '0';
        }

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