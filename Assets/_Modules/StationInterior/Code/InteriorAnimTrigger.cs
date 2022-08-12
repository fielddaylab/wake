using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.StationInterior {
    public sealed class InteriorAnimTrigger : MonoBehaviour, IBaked {
        public ParticleSystem[] IntroParticles;
        public Animator CraneAnimator;
        public SerializedHash32 CraneSFX;

        private void Start() {
            Script.OnSceneLoad(PlayAnim);
            #if UNITY_EDITOR
            if (Application.IsPlaying(this)) {
                return;
            }
            #endif // UNITY_EDITOR
            DisableAnim();
        }

        private void DisableAnim() {
            foreach(var ps in IntroParticles) {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            CraneAnimator.Play("Crane_PickUp", -1, 0);
            CraneAnimator.enabled = false;
        }

        private void PlayAnim() {
            CraneAnimator.enabled = true;
            foreach(var ps in IntroParticles) {
                ps.Play();
            }
            Services.Audio.PostEvent(CraneSFX);
        }

        #if UNITY_EDITOR

        int IBaked.Order => 0;

        bool IBaked.Bake(BakeFlags flags) {
            IntroParticles = GameObject.Find("WaterRings")?.GetComponentsInChildren<ParticleSystem>();
            CraneAnimator = GameObject.Find("StationInterior_Crane")?.GetComponent<Animator>();
            if ((flags & BakeFlags.IsBuild) == 0) {
                DisableAnim();
            }
            return true;
        }

        private void Reset() {
            IntroParticles = GameObject.Find("WaterRings")?.GetComponentsInChildren<ParticleSystem>();
            CraneAnimator = GameObject.Find("StationInterior_Crane")?.GetComponent<Animator>();
        }

        #endif // UNITY_EDITOR
    }
}