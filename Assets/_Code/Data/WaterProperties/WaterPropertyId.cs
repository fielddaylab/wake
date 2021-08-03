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
        MAX,

        [Hidden]
        TRACKED_MAX = CarbonDioxide,
        [Hidden]
        TRACKED_COUNT = TRACKED_MAX + 1,
        [Hidden]
        NONE = MAX,
    }
}