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
        static public readonly TableKeyPair ViewId = TableKeyPair.Parse("temp:view");

        // session

        // jobs
        static public readonly TableKeyPair CurrentJob = TableKeyPair.Parse("player:currentJob");
        static public readonly TableKeyPair CurrentStation = TableKeyPair.Parse("player:currentStation");

        // currency
        static public readonly TableKeyPair PlayerCash = TableKeyPair.Parse("player:cash");
        static public readonly TableKeyPair PlayerExp = TableKeyPair.Parse("player:exp");
        static public readonly TableKeyPair PlayerLevel = TableKeyPair.Parse("player:expLevel");

        // global
        static public readonly TableKeyPair PlayerGender = TableKeyPair.Parse("player:gender");
        static public readonly TableKeyPair SceneName = TableKeyPair.Parse("scene:name");
        static public readonly TableKeyPair MapId = TableKeyPair.Parse("scene:mapId");
        static public readonly TableKeyPair LastEntrance = TableKeyPair.Parse("scene:lastEntrance");
        static public readonly TableKeyPair ActNumber = TableKeyPair.Parse("global:actNumber");
    }

    static public class GameConsts
    {
        static public readonly StringHash32 Target_V1ctor = "guide";
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