using System.Collections;
using UnityEngine;
using BeauRoutine;
using BeauUtil;
using Aqua.Cameras;

namespace Aqua.StationMap
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Animation m_Animation = null;
        [SerializeField] private ParticleSystem[] m_TrackedParticles = null;
        [SerializeField] private TrailRenderer[] m_TrackedTrails = null;

        private Routine m_DiveRoutine;

        private void OnEnable()
        {
            Services.Events.Register(DiveSite.Event_Dive, StartDiving, this);
        }

        private void OnDisable()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void StartDiving()
        {
            m_DiveRoutine.Replace(this, DiveRoutine());
        }

        public void OnTeleport()
        {
            foreach(var particleSystem in m_TrackedParticles)
            {
                if (particleSystem.isPlaying) {
                    particleSystem.Stop();
                    particleSystem.Play();
                }
            }

            foreach(var trail in m_TrackedTrails)
            {
                trail.Clear();
            }
        }

        private IEnumerator DiveRoutine()
        {
            Services.Audio.PostEvent("LowerSubFoley");
            m_Animation.Play();
            while(m_Animation.isPlaying)
                yield return null;
        }
    }
}