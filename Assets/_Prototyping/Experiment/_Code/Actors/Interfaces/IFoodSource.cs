using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    public interface IFoodSource
    {
        StringHash32 Id { get; }
        Transform Transform { get; }

        bool HasTag(StringHash32 inTag);
        float EnergyRemaining { get; }

        bool TryGetEatLocation(ActorCtrl inActor, out Transform outTransform, out Vector3 outOffset);
        void Bite(ActorCtrl inActor, float inBite);
    }
}