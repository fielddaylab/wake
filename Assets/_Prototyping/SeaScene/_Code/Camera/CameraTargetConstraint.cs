using System;
using BeauUtil;
using UnityEngine;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class CameraTargetConstraint : MonoBehaviour
    {
        #region Events

        private void OnEnable()
        {
            ObservationServices.Camera.SetTarget(transform);
        }

        private void OnDisable()
        {
            if (ObservationServices.Camera != null)
                ObservationServices.Camera.ClearTarget();
        }

        #endregion // Events
    }
}