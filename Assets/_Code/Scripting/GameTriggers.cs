using BeauUtil;

namespace Aqua
{
    static public class GameTriggers
    {
        static public readonly StringHash32 RequestPartnerHelp = "RequestPartnerHelp";
        
        static public readonly StringHash32 PlayerDream = "PlayerDream";

        static public readonly StringHash32 InspectObject = "InspectObject";
        static public readonly StringHash32 SceneStart = "SceneStart";

        static public readonly StringHash32 BestiaryEntryAdded = "BestiaryEntryAdded";
        static public readonly StringHash32 BestiaryFactAdded = "BestiaryFactAdded";
        static public readonly StringHash32 BestiaryFactAddedToModel = "BestiaryFactAddedToModel";

        static public readonly StringHash32 PortableOpened = "PortableOpened";
        static public readonly StringHash32 PortableAppOpened = "PortableAppOpened";

        static public readonly StringHash32 JobStarted = "JobStarted";
        static public readonly StringHash32 JobSwitched = "JobSwitched";
        static public readonly StringHash32 JobCompleted = "JobCompleted";
        static public readonly StringHash32 JobTaskCompleted = "JobTaskCompleted";
        static public readonly StringHash32 JobTasksUpdated = "JobTaskUpdated";

        static public readonly StringHash32 TryExitScene = "TryExitScene";
        static public readonly StringHash32 PlayerEnterRegion = "PlayerEnterRegion";
        static public readonly StringHash32 PlayerExitRegion = "PlayerExitRegion";
    }
}