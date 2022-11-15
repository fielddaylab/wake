using BeauUtil;
using UnityEngine;

namespace Aqua {
    public class ActivateWhenLoaded : MonoBehaviour {
        [SerializeField] private Behaviour[] m_Behaviours = null;
        [SerializeField] private ParticleSystem[] m_ParticleSystems = null;

        private void Awake() {
            SetState(false);

            Script.OnSceneLoad(OnSceneLoaded);
        }

        private void OnSceneLoaded() {
            SetState(true);
        }

        private void SetState(bool state) {
            foreach(var behavior in m_Behaviours) {
                behavior.enabled = state;
            }

            foreach(var particle in m_ParticleSystems) {
                var emission = particle.emission;
                emission.enabled = state;
            }
        }

        #if UNITY_EDITOR

        private void Reset() {
            m_Behaviours = GetComponents<Behaviour>();
            ArrayUtils.Remove(ref m_Behaviours, this);
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>();
        }

        #endif // UNITY_EDITOR
    }
}