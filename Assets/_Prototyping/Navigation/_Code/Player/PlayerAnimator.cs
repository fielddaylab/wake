using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using System;
using BeauUtil;
using Aqua;

namespace ProtoAqua.Navigation {
public class PlayerAnimator : MonoBehaviour {

        [SerializeField] Transform boatRenderer = null;
        [SerializeField] private Color m_DiveColor = Color.black;

        private Routine bobbingRoutine;
        private Routine drivingRoutine;

        [NonSerialized] private bool isBobbing = false;
        [NonSerialized] private bool isDriving = false;

        private Routine m_DiveRoutine;

        private void OnEnable()
        {
            Services.Events.Register(UIController.Event_Dive, StartDiving, this);
        }

        private void OnDisable()
        {
            Services.Events?.DeregisterAll(this);
        }

        public void HandleBobbing(Vector2 direction) {
            if (m_DiveRoutine)
                return;

            if(direction == Vector2.zero) {
                StartBobbing();
                StopDriving();
            } else {
                StopBobbing();
                StartDriving();
            }
        }

        private void StartBobbing() {
            if(!isBobbing){
                    bobbingRoutine = Routine.Start(this, BobbingRoutine());
                    isBobbing = true;
            }
                    
        }

        private void StopBobbing() {
             if(isBobbing) {
                 bobbingRoutine.Stop();
                 isBobbing = false; 
             }
        }


        private void StartDriving() {
            if(!isDriving) {
                drivingRoutine = Routine.Start(this, DrivingRoutine());
                isDriving = true;
            }
        }
        
        private void StopDriving() {
            if(isDriving) {
                drivingRoutine.Stop();
                isDriving = false;
            }
        }

        private void StartDiving()
        {
            m_DiveRoutine.Replace(this, DiveRoutine());
        }

        private IEnumerator DiveRoutine()
        {
            ColorGroup group = boatRenderer.GetComponent<ColorGroup>();
            CameraFOVPlane fovPlane = Services.State.Camera.GetComponent<CameraFOVPlane>();

            yield return Routine.Combine(
                boatRenderer.MoveTo(5, 3, Axis.Z, Space.Self).Ease(Curve.QuadIn),
                Tween.Color(group.Color, m_DiveColor, group.SetColor, 3).Ease(Curve.QuadIn),
                Tween.Float(fovPlane.Zoom, 3, (f) => fovPlane.Zoom = f, 3).Ease(Curve.QuadIn).DelayBy(0.5f)
            );

            yield return Routine.WaitForever();
        }

        private IEnumerator BobbingRoutine() {
            while(isBobbing) {
                //TODO change order of this, based on Z position?
                yield return boatRenderer.MoveTo(0f,2f, Axis.Z).Ease(Curve.Smooth);
                yield return boatRenderer.MoveTo(-.5f,2f, Axis.Z).Ease(Curve.Smooth);
            }
        }

        
        private IEnumerator DrivingRoutine() {
            yield return boatRenderer.MoveTo(-.5f,2f, Axis.Z).Ease(Curve.Smooth);
        }
    }
}