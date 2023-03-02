using System;
using System.Collections;
using BeauPools;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.U2D.Animation;
using Aqua.Option;
using System.Collections.Generic;

namespace Aqua.Animation
{
    public class StaggeredCharacterSkinning : MonoBehaviour, IBaked {
        public int Period = 2;
        public int Stagger = 0;
        public Animator Animator;

        [SerializeField] private SpriteSkin[] m_SkinnedSprites = null;

        public void OnEnable() {
            if (Script.IsLoading) {
                Script.OnSceneLoad(() => Refresh(GameQuality.Animation));
            } else {
                Refresh(GameQuality.Animation);
            }

            GameQuality.OnAnimationChanged.Register(Refresh);
        }

        public void OnDisable() {
            GameQuality.OnAnimationChanged.Deregister(Refresh);
        }

        private void Refresh(OptionsPerformance.FeatureMode mode) {
            if (!isActiveAndEnabled) {
                return;
            }

            bool hasAnims = mode != OptionsPerformance.FeatureMode.Low;
            bool fullAnims = mode == OptionsPerformance.FeatureMode.High;
            Animator.enabled = hasAnims;

            if (!hasAnims) {
                Animator.WriteDefaultValues();
            }

            int period = fullAnims ? 1 : Math.Max(1, Math.Min(Period, m_SkinnedSprites.Length));

            for(int i = 0; i < m_SkinnedSprites.Length; i++) {
                m_SkinnedSprites[i].enabled = hasAnims;
                m_SkinnedSprites[i].SetStaggeredUpdate(period, (Stagger + i) % period);
            }
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            if (!Frame.IsActive(this)) {
                return;
            }

            if (!Application.isPlaying || Script.IsLoading) {
                return;
            }

            Refresh(GameQuality.Animation);
        }

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            List<SpriteSkin> skinned = new List<SpriteSkin>(12);
            GetComponentsInChildren<SpriteSkin>(skinned);
            skinned.RemoveAll((s) => !s.enabled);
            // TODO: sort this into roughly equal buckets according to period and number of bones per skinned sprite?
            m_SkinnedSprites = skinned.ToArray();
            if (!Animator) {
                Animator = GetComponentInChildren<Animator>(true);
            }
            return true;
        }

        #endif // UNITY_EDITOR
    }
}