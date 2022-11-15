using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    public class KinematicDrag2D : MonoBehaviour {
        #region Inspector

        [Header("Components")]

        [Required(ComponentLookupDirection.Children)] public Collider2D Collider;

        [Header("Collisions")]

        public LayerMask TrackingMask;

        [Header("Force")]

        public float Drag = 1;

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;

        private void Awake() {
            m_Listener = WorldUtils.ListenForLayerMask(Collider, TrackingMask, OnAdd, OnRemove);
        }

        private void OnAdd(Collider2D collider) {
            KinematicObject2D k2d = collider.attachedRigidbody.GetComponent<KinematicObject2D>();
            if (k2d) {
                k2d.AdditionalDrag += Drag;
            }
        }

        private void OnRemove(Collider2D collider) {
            if (!collider) {
                return;
            }

            KinematicObject2D k2d = collider.attachedRigidbody.GetComponent<KinematicObject2D>();
            if (k2d) {
                k2d.AdditionalDrag -= Drag;
            }
        }
    }
}