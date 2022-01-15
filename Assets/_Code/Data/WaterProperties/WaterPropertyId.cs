using BeauUtil;

namespace Aqua
{
    [LabeledEnum(false)]
    public enum WaterPropertyId : byte
    {
        Oxygen,
        Temperature, // environmental
        Light, // calculated
        PH,
        CarbonDioxide,

        [Hidden]
        _Unused, // environmental

        Food, // food (for eating)
        Mass, // mass (for growth)

        [Hidden]
        COUNT,

        [Hidden]
        TRACKED_COUNT = CarbonDioxide + 1,
        [Hidden]
        NONE = COUNT,
    }

    static public class WaterProperties
    {
        public const WaterPropertyId TrackedMax = WaterPropertyId.CarbonDioxide;
    }
}