namespace Aqua
{
    public enum DayPhase : byte
    {
        Morning,
        Day,
        Evening,
        Night
    }

    public enum DaySubPhase : byte
    {
        NightToMorning,
        Morning,
        MorningToDay,
        Day,
        DayToEvening,
        Evening,
        EveningToNight,
        Night
    }

    static public class DayPhaseExtensions
    {
        static public bool IsTransitioning(this DaySubPhase inSubPhase)
        {
            switch(inSubPhase)
            {
                case DaySubPhase.NightToMorning:
                case DaySubPhase.MorningToDay:
                case DaySubPhase.DayToEvening:
                case DaySubPhase.EveningToNight:
                    return true;
                
                default:
                    return false;
            }
        }
    }
}