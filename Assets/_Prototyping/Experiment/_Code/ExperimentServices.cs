using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Experiment
{
    public class ExperimentServices : Services
    {
        static private ActorCoordinator s_CachedActorCoordinator;
        static public ActorCoordinator Actors
        {
            get { return RetrieveOrFind(ref s_CachedActorCoordinator, ServiceIds.AI); }
        }

        static private BehaviorCaptureControl s_CachedBehaviorCapture;
        static public BehaviorCaptureControl BehaviorCapture
        {
            get { return RetrieveOrFind(ref s_CachedBehaviorCapture, BehaviorCaptureControl.GlobalServiceId); }
        }
    }
}