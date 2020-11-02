using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua
{
    static public class GameEvents
    {
        static public readonly StringHash32 CutsceneStart = "cutscene:start";
        static public readonly StringHash32 CutsceneEnd = "cutscene:end";

        static public readonly StringHash32 KevinChatterStart = "kevin:chatter-start";
        static public readonly StringHash32 KevinChatterEnd = "kevin:chatter-end";
    }
}