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
        static public readonly TableKeyPair SceneName = TableKeyPair.Parse("scene:name");
        static public readonly TableKeyPair MapId = TableKeyPair.Parse("scene:mapId");
        static public readonly TableKeyPair LastEntrance = TableKeyPair.Parse("scene:lastEntrance");
        static public readonly TableKeyPair ActNumber = TableKeyPair.Parse("global:actNumber");

        // save data
        static public readonly TableKeyPair TotalPlayTime_Seconds = TableKeyPair.Parse("time:seconds");
        static public readonly TableKeyPair TotalPlayTime_Minutes = TableKeyPair.Parse("time:minutes");
    }

    static public class GameConsts
    {
        static public readonly StringHash32 Target_V1ctor = "guide";
        static public readonly StringHash32 Target_Player = "player";

        public const int GameSceneIndexStart = 2;
    }

    static public class GameStats
    {
        static public readonly TableKeyPair Dive_TotalTime = TableKeyPair.Parse("player:stats.dive.totalTime");
    }
}