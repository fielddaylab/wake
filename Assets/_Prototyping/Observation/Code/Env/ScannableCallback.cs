using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Events;

namespace ProtoAqua.Observation {
    [RequireComponent(typeof(ScannableRegion))]
    public class ScannableCallback : MonoBehaviour {
        public float Delay;
        public UnityEvent OnScan;

        private void Awake() {
            ScannableRegion region = GetComponent<ScannableRegion>();
            region.OnScanComplete += (s) => {
                if (s != ScanResult.NoChange) {
                    if (Delay > 0) {
                        Routine.StartDelay(this, Trigger, Delay);
                    } else {
                        Trigger();
                    }
                }
            };
        }

        private void Trigger() {
            OnScan.Invoke();
        }
    }
}