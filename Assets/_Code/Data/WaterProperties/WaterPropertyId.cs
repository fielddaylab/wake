using BeauUtil;

namespace Aqua
{
    [LabeledEnum(false)]
    public enum WaterPropertyId : byte
    {
        Oxygen,
        Temperature,
        Light,
        PH,
        CarbonDioxide,
        Salinity,

        Food, // food (for eating)
        Mass, // mass (for growth)

        [Hidden]
        MAX,

        [Hidden]
        TRACKED_MAX = Salinity
    }
}