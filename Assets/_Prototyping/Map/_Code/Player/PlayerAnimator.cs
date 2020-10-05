using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;

namespace ProtoAqua.Map {
public class PlayerAnimator : MonoBehaviour {

        [SerializeField] Transform boatRenderer = null;

        private Routine bobbingRoutine;
        private bool isBobbing = false;

        // Start is called before the first frame update
        void Start() {
            
        }

        // Update is called once per frame
        void Update() {
            
        }

        public void HandleBobbing(Vector2 direction) {
            if(direction == Vector2.zero) {
                StartBobbing();
            } else {
                StopBobbing();
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
                

        private IEnumerator BobbingRoutine() {
            while(isBobbing) {
                //TODO change order of this, based on Z position?
                yield return boatRenderer.MoveTo(-.4f,1f, Axis.Z).Ease(Curve.Smooth);
                yield return boatRenderer.MoveTo(0f,1f, Axis.Z).Ease(Curve.Smooth);
            }
        }
    }
}