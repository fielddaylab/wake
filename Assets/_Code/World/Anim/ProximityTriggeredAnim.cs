using System;
using BeauUtil;
using UnityEngine;

namespace Aqua.Animation
{
    public sealed class ProximityTriggeredAnim : MonoBehaviour {
        [Required(ComponentLookupDirection.Self)] public Animator Animator;
        [Required(ComponentLookupDirection.Children)] public Collider2D Collider;
        public LayerMask CollisionMask = GameLayers.Player_Mask;
        public AnimatorParamChange OnEnter;
        public AnimatorParamChange OnExit;

        [NonSerialized] public TriggerListener2D Listener;

        private void Awake() {
            WorldUtils.ListenForLayerMask(Collider, CollisionMask, (c) => {
                OnEnter.Apply(Animator);
            }, (c) => {
                OnExit.Apply(Animator);
            });
        }
    }
}