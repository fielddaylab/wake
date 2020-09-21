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
    public interface IActorAction
    {
        float EstimateCost(ActorCtrl inActor, IActorGoal inGoal, VariantTable inVars);
    }
}