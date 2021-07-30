using System;
using BeauRoutine;
using BeauUtil;
using UnityEditor;
using UnityEngine;
using BeauPools;

namespace Aqua
{
    public class VFX : MonoBehaviour, IPooledObject<VFX>
    {
        [Serializable] public sealed class Pool : SerializablePool<VFX> { }

        [Required(ComponentLookupDirection.Self)] public Transform Transform;
        public SpriteRenderer Sprite;
        public ParticleSystem Particles;

        [NonSerialized] public Routine Animation;
        private IPool<VFX> m_Source;

        public void Free()
        {
            m_Source.Free(this);
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
    }
}