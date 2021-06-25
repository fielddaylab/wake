using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using System;
using BeauUtil;
using Aqua;

namespace Aqua.StationMap {
public class PlayerAnimator : MonoBehaviour {

        [SerializeField] Transform boatRenderer = null;
        [SerializeField] private Color m_DiveColor = Color.black;

        private Routine m_DiveRoutine;

        private void OnEnable()
        {
            Services.Events.Register(NavigationUI.Event_Dive, StartDiving, this);
        }

        private void OnDisable()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void StartDiving()
        {
            m_DiveRoutine.Replace(this, DiveRoutine());
        }

        private IEnumerator DiveRoutine()
        {
            ColorGroup group = boatRenderer.GetComponent<ColorGroup>();
            CameraFOVPlane fovPlane = Services.Camera.Rig.FOVPlane;

            yield return Routine.Combine(
                boatRenderer.MoveTo(5, 3, Axis.Z, Space.Self).Ease(Curve.QuadIn),
                Tween.Color(group.Color, m_DiveColor, group.SetColor, 3).Ease(Curve.QuadIn),
                Tween.Float(fovPlane.Zoom, 3, (f) => fovPlane.Zoom = f, 3).Ease(Curve.QuadIn).DelayBy(0.5f)
            );

            yield return Routine.WaitForever();
        }
    }
}