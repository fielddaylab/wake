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

        // session
        static public readonly TableKeyPair DiveSite = TableKeyPair.Parse("session:nav.diveSite");
        static public readonly TableKeyPair ShipRoom = TableKeyPair.Parse("session:nav.shipRoom");

        // jobs
        static public readonly TableKeyPair CurrentJob = TableKeyPair.Parse("player:currentJob");

        // global
        static public readonly TableKeyPair Weekday = TableKeyPair.Parse("date:weekday");
        static public readonly TableKeyPair PlayerGender = TableKeyPair.Parse("player:gender");
        static public readonly TableKeyPair SceneName = TableKeyPair.Parse("scene:name");
        static public readonly TableKeyPair ActNumber = TableKeyPair.Parse("global:actNumber");
    }
}