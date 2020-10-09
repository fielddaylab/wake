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
        bool HasTag(StringHash32 inTag);
        float EnergyRemaining { get; }

        void Bite(ActorCtrl inActor, float inBite);
    }
}