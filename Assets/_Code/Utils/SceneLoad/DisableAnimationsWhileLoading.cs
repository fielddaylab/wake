using UnityEngine;
using ScriptableBake;
using UnityEngine.UI;
using BeauUtil;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;

namespace Aqua
{
    public class DisableAnimationsWhileLoading : MonoBehaviour, IBaked, ISceneLoadHandler
    {
        [SerializeField] private Animator[] m_Animators = null;
        [SerializeField] private SpriteSkin[] m_SkinnedSprites = null;
        [SerializeField] private ParticleSystem[] m_ParticleSystems = null;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            SetState(true);
        }
        
        private void SetState(bool state) {
            foreach(var animator in m_Animators) {
                animator.enabled = state;
            }

            foreach(var skin in m_SkinnedSprites) {
                skin.enabled = state;
            }

            foreach(var particle in m_ParticleSystems) {
                var emission = particle.emission;
                emission.enabled = state;
                if (Application.isPlaying) {
                    if (state) {
                        particle.Play();
                    } else {
                        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 1000000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            List<Animator> animators = new List<Animator>(100);
            context.Scene.GetAllComponents<Animator>(animators);
            animators.RemoveAll((a) => !a.enabled);

            m_Animators = animators.ToArray();

            List<SpriteSkin> skins = new List<SpriteSkin>(100);
            context.Scene.GetAllComponents<SpriteSkin>(skins);
            skins.RemoveAll((a) => !a.enabled);

            m_SkinnedSprites = skins.ToArray();

            List<ParticleSystem> particles = new List<ParticleSystem>(100);
            context.Scene.GetAllComponents<ParticleSystem>(particles);
            particles.RemoveAll((p) => !p.emission.enabled || !p.main.playOnAwake || !p.main.loop); // must be emitting, play on awake, and looping

            m_ParticleSystems = particles.ToArray();

            SetState(false);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}