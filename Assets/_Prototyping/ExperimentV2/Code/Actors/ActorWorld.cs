using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using BeauPools;
using Aqua.Animation;
using Aqua;
using System.Collections;

namespace ProtoAqua.ExperimentV2
{
    public sealed class ActorWorld
    {
        public readonly Bounds WorldBounds;
        public readonly Action<ActorInstance> Free;

        public ActorWorld(Bounds inBounds, Action<ActorInstance> inFree)
        {
            WorldBounds = inBounds;
            Free = inFree;
        }
    }
}