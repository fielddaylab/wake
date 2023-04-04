using Aqua;
using Aqua.Compression;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    public class ROVInterfaceResponses : MonoBehaviour, IUpdaterUI {
        public RectTransform Offset;
        public CanvasGroup Alpha;

        private float m_AlphaFlickerDuration;
        private float m_ShakeDuration;
        private Vector2 m_ShakeVector;

        private void Awake() {
            Services.Events.Register(PlayerROVBreaker.Event_OnFire, OnBreakerFire)
                .Register<Vector2>(PlayerROV.Event_DashCollision, OnDashCollision)
                .Register<Vector2>(PlayerROV.Event_HardCollision, OnHardCollision);
        }

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        private void OnBreakerFire() {
            if (Accessibility.Photosensitive) {
                return;
            }
            
            m_AlphaFlickerDuration = 1;
        }

        private void OnDashCollision(Vector2 normal) {
            if (Accessibility.ReduceCameraMovement) {
                return;
            }

            m_ShakeVector = normal * 3 + RNG.Instance.NextVector2(0.4f, 0.5f);
            m_ShakeDuration = 1;
        }

        private void OnHardCollision(Vector2 normal) {
            if (Accessibility.ReduceCameraMovement) {
                return;
            }

            m_ShakeVector = normal * 3 + RNG.Instance.NextVector2(0.4f, 0.5f);
            m_ShakeDuration = 0.8f;
        }

        public void OnUIUpdate() {
            if (m_AlphaFlickerDuration > 0) {
                m_AlphaFlickerDuration -= Time.deltaTime;
                if (m_AlphaFlickerDuration > 0) {
                    if (Frame.Interval(4)) {
                        Alpha.alpha = RNG.Instance.NextFloat(0.7f, 0.9f) * CompressionRange.Quantize(Mathf.Max(1 - m_AlphaFlickerDuration, 0.3f), 0.2f);
                    }
                } else {
                    Alpha.alpha = 1;
                }
            }

            Vector2 offset = default;

            if (m_ShakeDuration > 0) {
                m_ShakeDuration -= Time.deltaTime;
                if (m_ShakeDuration > 0) {
                    float x = Mathf.Cos(m_ShakeDuration * Mathf.PI * 8) * m_ShakeDuration;
                    float y = Mathf.Sin((m_ShakeDuration * 0.77f) * Mathf.PI * 8 * 3.7f) * m_ShakeDuration;
                    offset += new Vector2(m_ShakeVector.x * x, m_ShakeVector.y * y);
                }
            }

            Offset.anchoredPosition = offset;
        }
    }
}