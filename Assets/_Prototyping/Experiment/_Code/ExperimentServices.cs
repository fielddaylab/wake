using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using UnityEngine;

namespace ProtoAqua.Experiment
{
    public class ExperimentServices : Services
    {
        [ServiceReference] static public ActorCoordinator Actors { get; private set; }
        [ServiceReference] static public BehaviorCaptureControl BehaviorCapture { get; private set; }
    }
}