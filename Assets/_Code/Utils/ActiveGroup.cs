using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua {
    [Serializable]
    public sealed class ActiveGroup {
        public GameObject[] GameObjects = Array.Empty<GameObject>();
        public Behaviour[] Behaviours = Array.Empty<Behaviour>();

        [NonSerialized] public bool Active;

        public bool Empty {
            get { return GameObjects.Length == 0 && Behaviours.Length == 0; }
        }

        public bool Activate() {
            if (Active) {
                return false;
            }

            Active = true;

            foreach(var go in GameObjects) {
                go.SetActive(true);
            }
            foreach(var b in Behaviours) {
                b.enabled = true;
            }

            return true;
        }

        public bool Deactivate() {
            if (!Active) {
                return false;
            }

            Active = false;

            foreach(var go in GameObjects) {
                go.SetActive(false);
            }
            foreach(var b in Behaviours) {
                b.enabled = false;
            }

            return true;
        }

        public bool SetActive(bool active) {
            return active ? Activate() : Deactivate();
        }

        public void ForceActive(bool active) {
            Active = !active;
            if (active) {
                Activate();
            } else {
                Deactivate();
            }
        }
    }
}