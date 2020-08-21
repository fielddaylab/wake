using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    public class ObservationServices : Services
    {
        static private CameraCtrl s_CachedCamera;
        static public CameraCtrl Camera
        {
            get { return RetrieveOrFind(ref s_CachedCamera, ServiceIds.Camera); }
        }
    }
}