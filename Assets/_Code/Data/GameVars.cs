using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;

namespace Aqua
{
    static public class GameVars
    {
        // temporary
        static public readonly TableKeyPair CameraRegion = TableKeyPair.Parse("temp:camera.region");
        static public readonly TableKeyPair InteractObject = TableKeyPair.Parse("temp:interact.object");

        // session
        static public readonly TableKeyPair DiveSite = TableKeyPair.Parse("session:nav.diveSite");

        // jobs
        static public readonly TableKeyPair CurrentJob = TableKeyPair.Parse("player:currentJob");
        static public readonly TableKeyPair CurrentStation = TableKeyPair.Parse("player:currentStation");
        static public readonly TableKeyPair JobsAnyAvailable = TableKeyPair.Parse("jobs:anyAvailable");
        static public readonly TableKeyPair JobsAnyInProgress = TableKeyPair.Parse("jobs:anyInProgress");
        static public readonly TableKeyPair JobsAnyComplete = TableKeyPair.Parse("jobs:anyComplete");

        // global
        static public readonly TableKeyPair DayName = TableKeyPair.Parse("time:dayName");
        static public readonly TableKeyPair DayPhase = TableKeyPair.Parse("time:dayPhase");
        static public readonly TableKeyPair IsDay = TableKeyPair.Parse("time:isDay");
        static public readonly TableKeyPair IsNight = TableKeyPair.Parse("time:isNight");
        static public readonly TableKeyPair Hour = TableKeyPair.Parse("time:hour");
        static public readonly TableKeyPair DayNumber = TableKeyPair.Parse("time:dayNumber");

        static public readonly TableKeyPair PlayerGender = TableKeyPair.Parse("player:gender");
        static public readonly TableKeyPair SceneName = TableKeyPair.Parse("scene:name");
        static public readonly TableKeyPair ActNumber = TableKeyPair.Parse("global:actNumber");
        static public readonly TableKeyPair ShipRoom = TableKeyPair.Parse("global:nav.shipRoom");
    }

    static public class GameConsts
    {
        static public readonly StringHash32 CashId = "Cash";
        static public readonly StringHash32 GearsId = "Gear";

        static public readonly StringHash32 Target_Kevin = "kevin";
        static public readonly StringHash32 Target_Player = "player";

        static public readonly StringHash32 DayPhase_Morning = "morning";
        static public readonly StringHash32 DayPhase_Day = "day";
        static public readonly StringHash32 DayPhase_Evening = "evening";
        static public readonly StringHash32 DayPhase_Night = "night";

        static public readonly StringHash32 DayName_Sunday = "sunday";
        static public readonly StringHash32 DayName_Monday = "monday";
        static public readonly StringHash32 DayName_Tuesday = "tuesday";
        static public readonly StringHash32 DayName_Wednesday = "wednesday";
        static public readonly StringHash32 DayName_Thursday = "thursday";
        static public readonly StringHash32 DayName_Friday = "friday";
        static public readonly StringHash32 DayName_Saturday = "saturday";

        public const int GameSceneIndexStart = 2;
    }
}