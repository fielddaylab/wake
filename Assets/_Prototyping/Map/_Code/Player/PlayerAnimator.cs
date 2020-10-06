using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;

namespace ProtoAqua.Map {
public class PlayerAnimator : MonoBehaviour {

        [SerializeField] Transform boatRenderer = null;

        private Routine bobbingRoutine;
        private Routine drivingRoutine;
        private bool isBobbing = false;
        private bool isDriving = false;

        // Start is called before the first frame update
        void Start() {
            
        }

        // Update is called once per frame
        void Update() {
            
        }

        public void HandleBobbing(Vector2 direction) {
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
                
                

        private IEnumerator BobbingRoutine() {
            while(isBobbing) {
                //TODO change order of this, based on Z position?
                yield return boatRenderer.MoveTo(0f,2f, Axis.Z).Ease(Curve.Smooth);
                yield return boatRenderer.MoveTo(-1f,2f, Axis.Z).Ease(Curve.Smooth);
            }
        }

        
        private IEnumerator DrivingRoutine() {
            yield return boatRenderer.MoveTo(-1f,2f, Axis.Z).Ease(Curve.Smooth);
        }
    }
}