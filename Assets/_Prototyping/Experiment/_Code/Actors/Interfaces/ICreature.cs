using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    public interface ICreature
    {
        StringHash32 Id { get; }
        Transform Transform { get; }

        bool HasTag(StringHash32 inTag);

        bool TryGetEatLocation(ActorCtrl inActor, out Transform outTransform, out Vector3 outOffset);
        void Bite(ActorCtrl inActor, float inBite);
    }
}