using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using UnityEngine;

namespace ProtoAqua.Observation
{
    public class ObservationServices : Services
    {
        [ServiceReference] static public CameraCtrl Camera { get; private set; }
        [ServiceReference] static public ObservationUI SceneUI { get; private set; }
    }
}