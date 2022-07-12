using BeauUtil;
using UnityEngine;

namespace Aqua {
    static public class AQColors {
        static public readonly Color HighlightYellow = Colors.Hex("#ECD500");
        static public readonly Color DarkerYellow = Colors.Hex("#8D9412");
        
        static public readonly Color BrightBlue = Colors.Hex("#A9E0DD");
        static public readonly Color ContentBlue = Colors.Hex("#00E7DD");
        
        static public readonly Color LightTeal = Colors.Hex("#006864");
        static public readonly Color Teal = Colors.Hex("#065C55");
        static public readonly Color DarkTeal = Colors.Hex("#003935");
        static public readonly Color DarkerTeal = Colors.Hex("#003134");
        static public readonly Color DarkestTeal = Colors.Hex("#00272A");

        static public readonly Color LightRed = Colors.Hex("#FF8E6B");
        static public readonly Color Red = Colors.Hex("#FF6262");

        static public readonly Color Cash = Colors.Hex("#E4CF13");
        static public readonly Color Exp = Colors.Hex("#A2FE28");

        static public Color ForItem(StringHash32 inItemId, Color inDefault)
        {
            if (inItemId == ItemIds.Cash)
                return Cash;
            if (inItemId == ItemIds.Exp)
                return Exp;

            return inDefault;
        }
    }
}