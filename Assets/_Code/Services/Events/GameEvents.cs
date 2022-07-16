using BeauUtil;

namespace Aqua
{
    static public class GameEvents
    {
        static public readonly StringHash32 ProfileUnloaded = "profile:unloaded"; // no args
        static public readonly StringHash32 ProfileLoaded = "profile:loaded"; // no args
        static public readonly StringHash32 ProfileRefresh = "profile:refresh"; // no args

        static public readonly StringHash32 ProfileStarting = "profile:starting"; // string userName
        static public readonly StringHash32 ProfileStarted = "profile:started"; // no args
        
        static public readonly StringHash32 ProfileSaveBegin = "profile:save-begin"; // no args
        static public readonly StringHash32 ProfileSaveCompleted = "profile:save-completed"; // no args

        static public readonly StringHash32 ProfileAutosaveHint = "profile:autosave-hint"; // AutoSave.Mode mode
        static public readonly StringHash32 ProfileAutosaveSuppress = "profile:autosave-suppress"; // no args

        static public readonly StringHash32 OptionsUpdated = "profile:options-updated"; // OptionsData options

        static public readonly StringHash32 SceneWillUnload = "scene:will-unload"; // no args
        static public readonly StringHash32 ScenePreloading = "scene:preloading"; // no args
        static public readonly StringHash32 SceneLoaded = "scene:loaded"; // no args

        static public readonly StringHash32 ViewChanged = "view:changed"; // string viewName
        static public readonly StringHash32 ViewLockChanged = "view:lockChanged"; // no args

        static public readonly StringHash32 CutsceneStart = "cutscene:start"; // no args
        static public readonly StringHash32 CutsceneEnd = "cutscene:end"; // no args
        static public readonly StringHash32 CutsceneSkip = "cutscene:skip"; // no args
        static public readonly StringHash32 ScriptNodeSeen = "script:node-seen"; // StringHash32 nodeId
        static public readonly StringHash32 ScriptChoicePresented = "script:choice-presented"; // DialogRecord lastLine
        static public readonly StringHash32 ScriptFired = "script:fired"; // string nodeId

        static public readonly StringHash32 GuideChatterStart = "guide:chatter-start"; // no args
        static public readonly StringHash32 GuideChatterEnd = "guide:chatter-end"; // no args

        static public readonly StringHash32 BestiaryUpdated = "bestiary:updated"; // BestiaryUpdateParams bestiaryParams
        static public readonly StringHash32 InventoryUpdated = "inventory:updated"; // StringHash32 itemId
        static public readonly StringHash32 ScanLogUpdated = "scan-log:updated"; // StringHash32 scanId
        static public readonly StringHash32 SiteDataUpdated = "site-data:updated"; // StringHash32 siteId
        static public readonly StringHash32 ArgueDataUpdated = "argue:updated"; // StringHash32 argumentationId
        static public readonly StringHash32 ScienceLevelUpdated = "science:level-updated"; // ScienceLevelUp

        static public readonly StringHash32 VariableSet = "data:variable-set"; // TableKeyPair variableId

        static public readonly StringHash32 BeginDive = "dive:begin";
        static public readonly StringHash32 BeginArgument = "argument:begin";

        static public readonly StringHash32 PortableOpened = "portable:opened"; // PortableRequest request
        static public readonly StringHash32 PortableClosed = "portable:closed"; // no args
        static public readonly StringHash32 PortableAppOpened = "portable:app-opened"; // 
        static public readonly StringHash32 PortableAppClosed = "portable:app-closed";
        static public readonly StringHash32 PortableEntrySelected = "portable:bestiary-entrySelected";

        static public readonly StringHash32 PopupOpened = "popup:opened"; // no args
        static public readonly StringHash32 PopupClosed = "popup:closed"; // no args

        static public readonly StringHash32 ContextDisplay = "context:displayed"; // no args
        static public readonly StringHash32 ContextHide = "context:hide"; // no args

        static public readonly StringHash32 JobUnload = "job:unload"; // StringHash32 jobId
        static public readonly StringHash32 JobPreload = "job:preload"; // StringHash32 jobId

        static public readonly StringHash32 JobStarted = "job:started"; // StringHash32 jobId
        static public readonly StringHash32 JobPreComplete = "job:preComplete"; // StringHash32 jobId
        static public readonly StringHash32 JobCompleted = "job:completed"; // StringHash32 jobId
        static public readonly StringHash32 JobSwitched = "job:switched"; // StringHash32 jobId
        
        static public readonly StringHash32 JobTaskAdded = "job:task-added"; // StringHash32 taskId
        static public readonly StringHash32 JobTaskRemoved = "job:task-removed"; // StringHash32 taskId
        static public readonly StringHash32 JobTaskCompleted = "job:task-completed"; // StringHash32 taskId
        static public readonly StringHash32 JobTasksUpdated = "job:tasks-updated"; // no args

        static public readonly StringHash32 ActChanged = "act:changed"; // uint actIndex
        static public readonly StringHash32 StationChanged = "station:changed"; // StringHash32 stationId
        static public readonly StringHash32 LocationSeen = "location:seen"; // StringHash32 locationId
    }
}