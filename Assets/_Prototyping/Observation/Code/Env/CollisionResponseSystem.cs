using UnityEngine;
using Aqua;
using Aqua.Scripting;
using BeauUtil;
using System.Collections;
using BeauRoutine;
using System;
using ScriptableBake;
using System.Collections.Generic;
using AquaAudio;
using BeauUtil.Debugger;

namespace ProtoAqua.Observation
{
    [DefaultExecutionOrder(500)]
    public class CollisionResponseSystem : SharedManager, IScenePreloader, IBaked {
        public enum ImpactType {
            Light,
            Heavy,
            Slide
        }

        public enum ColorTintMode {
            Multiply,
            Lerp
        }

        [Serializable]
        public struct ColorTint {
            public Color Color;
            public ColorTintMode Mode;
            [Range(0, 1)] public float Factor;

            public Color Apply(Color c) {
                switch(Mode) {
                    case ColorTintMode.Multiply: {
                        return c * Color;
                    }
                    case ColorTintMode.Lerp: {
                        return Color.Lerp(c, Color, Factor);
                    }
                    default: {
                        return c;
                    }
                }
            }
        }

        [Serializable]
        public struct EffectResponse {
            [AutoEnum] public ColliderMaterialId Material;
            [AutoEnum] public ImpactType Type;
            public VFX Prefab;

            [Header("Sounds")]
            public SerializedHash32 Sound;
            [Range(0, 1)] public float SoundVolume;

            [NonSerialized] public VFX.Pool InstantiatedPool;
            [NonSerialized] public Vector3 OriginalShapeSize;
        }

        public struct Request {
            public ColliderMaterialId Material;
            public ImpactType Type;
            public Vector3 Position;
            public Vector3 Normal;
            public float Size;
        }

        [SerializeField] private EffectResponse[] m_Responses = null;
        [SerializeField] private ColorTint m_ColorTint = new ColorTint() {
            Color = Color.white,
            Mode = ColorTintMode.Multiply
        };
        
        [Header("Config")]
        [SerializeField, PrefabModeOnly] private Transform m_EffectPoolRoot = null;

        private readonly RingBuffer<VFX> m_ActiveEffects = new RingBuffer<VFX>(8, RingBufferMode.Expand);
        private readonly RingBuffer<Request> m_RequestQueue = new RingBuffer<Request>(16, RingBufferMode.Expand);
        private readonly Dictionary<VFX, VFX.Pool> m_PoolMap = new Dictionary<VFX, VFX.Pool>(8);

        protected override void Awake() {
            base.Awake();
        }

        private void LateUpdate() {
            if (Frame.Interval(3)) {
                m_ActiveEffects.RemoveWhere(VFX.FreeOnCompletedPredicate);
            }

            while(m_RequestQueue.Count > 0) {
                Request request = m_RequestQueue.PopFront();
                int responseIdx = FindResponseIndex(request.Material, request.Type);
                if (responseIdx >= 0) {
                    ref EffectResponse response = ref m_Responses[responseIdx];
                    VFX effect = response.InstantiatedPool.Alloc(request.Position, Quaternion.Euler(0, 0, (float) Math.Atan2(request.Normal.y, request.Normal.x) * Mathf.Rad2Deg), true);
                    if (effect.Particles) {
                        ParticleSystem.ShapeModule shape = effect.Particles.shape;
                        shape.scale = response.OriginalShapeSize * request.Size;
                        ParticleSystem.MainModule main = effect.Particles.main;
                        main.loop = false;
                    }
                    m_ActiveEffects.PushBack(effect);
                    effect.Play();

                    if (!response.Sound.IsEmpty) {
                        AudioHandle sfx = Services.Audio.PostEvent(response.Sound).SetPosition(request.Position);
                        if (response.SoundVolume > 0 && response.SoundVolume < 1) {
                            sfx.SetVolume(response.SoundVolume);
                        }
                    }
                }
            }
        }

        private int FindResponseIndex(ColliderMaterialId material, ImpactType type) {
            for(int i = 0; i < m_Responses.Length; i++) {
                ref EffectResponse response = ref m_Responses[i];
                if (response.Material == material && response.Type == type) {
                    return i;
                }
            }

            return -1;
        }

        public void Queue(ColliderMaterialId materialId, ImpactType type, Vector3 position, Vector3 normal, float size = 1) {
            m_RequestQueue.PushBack(new Request() {
                Material = materialId,
                Type = type,
                Position = position,
                Normal = normal,
                Size = size
            });
        }

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            for(int i = 0; i < m_Responses.Length; i++) {
                InitializeEffectResponse(ref m_Responses[i]);
                yield return null;
            }
        }

        private void InitializeEffectResponse(ref EffectResponse response) {
            if (response.Prefab.Particles) {
                response.OriginalShapeSize = response.Prefab.Particles.shape.scale;
            }

            VFX.Pool pool;
            if (!m_PoolMap.TryGetValue(response.Prefab, out pool)) {
                pool = new VFX.Pool();
                pool.Prefab = response.Prefab;
                pool.ConfigureCapacity(4, 1, true);
                pool.ConfigureTransforms(m_EffectPoolRoot);
                pool.TryInitialize(null, null, 0);

                pool.Config.RegisterOnConstruct((_, vfx) => {
                    if (vfx.Particles) {
                        var main = vfx.Particles.main;
                        var startingColor = main.startColor;
                        startingColor.colorMin = m_ColorTint.Apply(startingColor.colorMin);
                        startingColor.colorMax = m_ColorTint.Apply(startingColor.colorMax);
                        main.startColor = startingColor;
                    }
                });
            }

            response.InstantiatedPool = pool;
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 10000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            List<Collider2D> colliders = new List<Collider2D>(128);
            context.Scene.GetAllComponents<Collider2D>(true, colliders);
            HashSet<ColliderMaterialId> materials = Collections.NewSet<ColliderMaterialId>(8);
            colliders.ForEach((c) => {
                if (c.gameObject.layer != GameLayers.Solid_Index) {
                    return;
                }

                ColliderMaterialId matId = ColliderMaterial.Find(c);
                if (matId != ColliderMaterialId.Invisible) {
                    materials.Add(matId);
                }
            });

            HashSet<ColliderMaterialId> discardedMaterials = new HashSet<ColliderMaterialId>();

            for(int i = m_Responses.Length - 1; i >= 0; i--) {
                if (!materials.Contains(m_Responses[i].Material)) {
                    if (discardedMaterials.Add(m_Responses[i].Material)) {
                        Log.Msg("[CollisionResponseSystem] Material '{0}' is not present - discarding collision effects", m_Responses[i].Material);
                    }
                    ArrayUtils.RemoveAt(ref m_Responses, i);
                }
            }

            return true;
        }

        #endif // UNITY_EDITOR
    }
}