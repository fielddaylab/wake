using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    static public class GameEvents
    {
        static public readonly StringHash32 ProfileLoaded = "profile:loaded"; // no args
        static public readonly StringHash32 ProfileRefresh = "profile:refresh"; // no args

        static public readonly StringHash32 SceneWillUnload = "scene:will-unload"; // no args
        static public readonly StringHash32 ScenePreloading = "scene:preloading"; // no args
        static public readonly StringHash32 SceneLoaded = "scene:loaded"; // no args

        static public readonly StringHash32 CutsceneStart = "cutscene:start"; // no args
        static public readonly StringHash32 CutsceneEnd = "cutscene:end"; // no args
        static public readonly StringHash32 CutsceneSkip = "cutscene:skip"; // no args
        static public readonly StringHash32 ScriptNodeSeen = "script:node-seen"; // StringHash32 nodeId

        static public readonly StringHash32 KevinChatterStart = "kevin:chatter-start"; // no args
        static public readonly StringHash32 KevinChatterEnd = "kevin:chatter-end"; // no args

        static public readonly StringHash32 BestiaryUpdated = "bestiary:updated"; // BestiaryUpdateParams bestiaryParams
        static public readonly StringHash32 InventoryUpdated = "inventory:updated"; // StringHash32 itemId
        static public readonly StringHash32 ModelUpdated = "model:updated"; // StringHash32 factId
        static public readonly StringHash32 ScanLogUpdated = "scan-log:updated"; // StringHash32 scanId

        static public readonly StringHash32 VariableSet = "data:variable-set"; // TableKeyPair variableId

        static public readonly StringHash32 PortableOpened = "portable:opened"; // IPortableRequest request
        static public readonly StringHash32 PortableClosed = "portable:closed"; // no args

        static public readonly StringHash32 JobUnload = "job:unload"; // StringHash32 jobId
        static public readonly StringHash32 JobPreload = "job:preload"; // StringHash32 jobId

        static public readonly StringHash32 JobStarted = "job:started"; // StringHash32 jobId
        static public readonly StringHash32 JobCompleted = "job:completed"; // StringHash32 jobId
        static public readonly StringHash32 JobSwitched = "job:switched"; // StringHash32 jobId
        
        static public readonly StringHash32 JobTaskAdded = "job:task-added"; // StringHash32 taskId
        static public readonly StringHash32 JobTaskRemoved = "job:task-removed"; // StringHash32 taskId
        static public readonly StringHash32 JobTaskCompleted = "job:task-completed"; // StringHash32 taskId

        static public readonly StringHash32 ActChanged = "act:changed"; // uint actIndex
        static public readonly StringHash32 StationChanged = "station:changed"; // StringHash32 stationId
    }
}