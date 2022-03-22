using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua {
    public sealed class ActiveGroup : MonoBehaviour {
        public GameObject[] GameObjects;
        public Behaviour[] Behaviours;

        public bool SetActive(bool active) {
            if (gameObject.activeSelf == active) {
                return false;
            }

            gameObject.SetActive(active);
            return true;
        }

        private void OnEnable() {
            foreach(var go in GameObjects) {
                go.SetActive(true);
            }
            foreach(var b in Behaviours) {
                b.enabled = true;
            }
        }

        private void OnDisable() {
            foreach(var b in Behaviours) {
                b.enabled = false;
            }
            foreach(var go in GameObjects) {
                go.SetActive(false);
            }
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            for(int i = Behaviours.Length - 1; i >= 0; i--) {
                if (Behaviours[i] == this) {
                    Log.Warn("[ActiveGroup] Cannot include this behaviour or a parent transform in group");
                    ArrayUtils.RemoveAt(ref Behaviours, i);
                }
            }

            for(int i = GameObjects.Length - 1; i >= 0; i--) {
                if (GameObjects[i] == gameObject || IsSelfOrParent(GameObjects[i].transform, transform)) {
                    Log.Warn("[ActiveGroup] Cannot include this gameObject or a parent GameObject in group");
                    ArrayUtils.RemoveAt(ref GameObjects, i);
                }
            }
        }

        static private bool IsSelfOrParent(Transform parent, Transform here) {
            return parent == here || here.IsChildOf(parent);
        }

        #endif // UNITY_EDITOR
    }
}