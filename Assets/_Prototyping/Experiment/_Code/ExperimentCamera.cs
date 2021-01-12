using UnityEngine;
using BeauUtil;
using BeauUtil.Variants;
using System.Collections;
using BeauRoutine;
using System;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentCamera : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Transform m_Position = null;
        [SerializeField] private CameraFOVPlane m_CameraZoomer = null;

        [SerializeField] private Transform m_TankPosition = null;
        [SerializeField] private float m_TankZoom = 1.3f;
        [SerializeField] private TweenSettings m_TransitionSettings = new TweenSettings(0.5f, Curve.CubeInOut);

        #endregion // Inspector

        [NonSerialized] private Vector3 m_OriginalPosition;
        [NonSerialized] private Routine m_MoveRoutine;

        private void Awake()
        {
            m_OriginalPosition = m_Position.localPosition;

            Services.Events.Register(ExperimentEvents.ExperimentBegin, OnExperimentBegin, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnExperimentTeardown, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnExperimentBegin()
        {
            m_MoveRoutine.Replace(this, TransitionToTank());
        }

        private void OnExperimentTeardown()
        {
            m_MoveRoutine.Replace(this, TransitionToOriginal());
        }

        private IEnumerator TransitionToOriginal()
        {
            m_CameraZoomer.Zoom = 1;
            m_Position.localPosition = m_OriginalPosition;
            yield break;
        }

        private IEnumerator TransitionToTank()
        {
            yield return Routine.Combine(
                Tween.Float(m_CameraZoomer.Zoom, m_TankZoom, (f) => m_CameraZoomer.Zoom = f, m_TransitionSettings),
                m_Position.MoveTo(m_TankPosition.position, m_TransitionSettings, Axis.XY, Space.World)
            );
        }
    }
}