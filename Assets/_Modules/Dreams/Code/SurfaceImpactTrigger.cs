using UnityEngine;
using BeauUtil;
using AquaAudio;
using ProtoAqua.Observation;

namespace Aqua.Dreams {
    public class SurfaceImpactTrigger : MonoBehaviour {
        public CollisionResponseSystem.ImpactType ImpactType = CollisionResponseSystem.ImpactType.Heavy;
        public float ImpactScale = 1;

        [Header("Extra")]
        public Collider2D IgnoreCollider;
        public SerializedHash32 ImpactSFX;
        public float ScreenShake = 0;

        public void Play() {
            CollisionResponseSystem responseSystem = Services.State.FindManager<CollisionResponseSystem>();
            if (responseSystem == null) {
                return;
            }

            Vector3 pos = transform.position + new Vector3(0, 2, 0);
            RaycastHit2D solidHit = PhysicsService.SolidRaycast(pos, new Vector2(0, -1), 80, IgnoreCollider);
            if (!solidHit.collider) {
                return;
            }

            ColliderMaterialId material = ColliderMaterial.Find(solidHit.collider);;
            if (material == ColliderMaterialId.Invisible) {
                return;
            }

            pos.x = solidHit.point.x;
            pos.y = solidHit.point.y;

            responseSystem.Queue(material, ImpactType, pos, solidHit.normal, ImpactScale, CollisionResponseSystem.RequestFlags.DoNotOverride | (ImpactSFX.IsEmpty ? 0 : CollisionResponseSystem.RequestFlags.Silent));

            float distanceToCameraEdge = Mathf.Abs(pos.x - Services.Camera.Position.x);
            float shakeScale = Mathf.Max(0.1f, 1 - (distanceToCameraEdge / 15f));

            if (ScreenShake > 0) {
                Services.Camera.AddShake(ScreenShake * shakeScale, 0.2f, 0.8f);
            }

            if (!ImpactSFX.IsEmpty) {
                Services.Audio.PostEvent(ImpactSFX).SetPosition(pos);
            }
        }
    }
}