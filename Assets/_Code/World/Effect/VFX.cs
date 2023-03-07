using System;
using BeauRoutine;
using BeauUtil;
using UnityEditor;
using UnityEngine;
using BeauPools;
using UnityEngine.UI;
using System.Collections;

namespace Aqua
{
    public class VFX : MonoBehaviour, IPooledObject<VFX>
    {
        [Serializable] public sealed class Pool : SerializablePool<VFX> { }

        [Required(ComponentLookupDirection.Self)] public Transform Transform;
        public Graphic Graphic;
        public SpriteRenderer Sprite;
        public ParticleSystem Particles;

        [NonSerialized] public Routine Animation;
        private IPool<VFX> m_Source;

        public void Play() {
            if (Particles != null) {
                Particles.Play();
            }
        }

        public void Play(IEnumerator anim) {
            if (Particles != null) {
                Particles.Play();
            }

            if (anim != null) {
                Animation.Replace(this, anim);
            }
        }

        public void Free()
        {
            m_Source.Free(this);
        }

        public bool IsCompleted() {
            if (Particles != null && Particles.IsAlive(true)) {
                return false;
            }

            return !Animation;
        }

        #region IPooledObject

        void IPooledObject<VFX>.OnConstruct(IPool<VFX> inPool)
        {
            m_Source = inPool;
        }
        void IPooledObject<VFX>.OnDestruct() { }
        void IPooledObject<VFX>.OnAlloc() { }
        void IPooledObject<VFX>.OnFree() { Animation.Stop(); }

        #endregion // IPooledObject

        #if UNITY_EDITOR

        private void Reset()
        {
            Transform = transform;
            Sprite = GetComponentInChildren<SpriteRenderer>();
            Particles = GetComponentInChildren<ParticleSystem>();
        }

        #endif // UNITY_EDITOR

        static public readonly Predicate<VFX> CompletedPredicate = (v) => {
            return v.IsCompleted();
        };

        static public readonly Predicate<VFX> FreeOnCompletedPredicate = (v) => {
            if (v.IsCompleted()) {
                v.Free();
                return true;
            }

            return false;
        };
    }
}