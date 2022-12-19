using System;
using System.Collections;
using Aqua;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    public class TankWaterSystem : MonoBehaviour {
        private const float MaxWaterPitch = 3f;
        private const Curve WaterPitchCurve = Curve.CubeIn;

        #region Inspector

        [SerializeField, Required] private ParticleSystem m_FillParticles = null;
        [SerializeField, Required] private ParticleSystem m_SplashDownParticles = null;
        [SerializeField, Required] private ParticleSystem m_UnderwaterParticles = null;
        [SerializeField, Required] private GameObject m_FillParticleForceGroup = null;

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_WaterSFX;

        public void InitializeTank(SelectableTank inTank) {
            WorldUtils.ListenForLayerMask(inTank.WaterTrigger, GameLayers.Critter_Mask, (c) => OnWaterEnter(inTank, c), null);

            inTank.WaterSystem = this;
        }

        public void SetActiveTank(SelectableTank inTank) {
            var downParticleTrigger = m_SplashDownParticles.trigger;
            downParticleTrigger.SetCollider(0, inTank.WaterCollider3D);
        }

        #region Splash

        private void OnWaterEnter(SelectableTank inTank, Collider2D inCreature) {
            ActorInstance actor = inCreature.GetComponentInParent<ActorInstance>();
            actor.InWater = true;

            if (actor.CurrentAction == ActorActionId.BeingBorn) {
                UnderwaterPulse(inTank, actor, inCreature);
            } else {
                switch (actor.Definition.Spawning.SpawnAnimation) {
                    case ActorDefinition.SpawnAnimationId.Drop: {
                            actor.ActionAnimation.SetTimeScale(0.6f);
                            SurfaceSplash(inTank, actor, inCreature);
                            break;
                        }

                    case ActorDefinition.SpawnAnimationId.Sprout: {
                            UnderwaterPulse(inTank, actor, inCreature);
                            break;
                        }
                }
            }
        }

        private void SurfaceSplash(SelectableTank inTank, ActorInstance inActor, Collider2D inCollider) {
            Vector3 critterPosition = inCollider.transform.position;
            Rect critterLocalBounds = inActor.Definition.LocalBoundsRect;
            float splashCenterX = critterLocalBounds.center.x;

            Vector3 splashPosition;
            splashPosition.x = critterPosition.x + splashCenterX;
            splashPosition.y = inTank.Bounds.max.y;
            splashPosition.z = critterPosition.z;

            ParticleSystem.ShapeModule downShape = m_SplashDownParticles.shape;
            Vector3 downSize = downShape.scale;
            downSize.x = critterLocalBounds.width;
            downShape.scale = downSize;

            ParticleSystem.EmitParams emit = default;
            emit.position = splashPosition;
            emit.applyShapeToPosition = true;

            m_SplashDownParticles.Emit(emit, 64);

            Services.Audio.PostEvent("tank_water_splash");
        }

        private void UnderwaterPulse(SelectableTank inTank, ActorInstance inActor, Collider2D inCollider) {
            Vector3 critterPosition = inCollider.transform.position;
            Rect critterLocalBounds = inActor.Definition.LocalBoundsRect;
            Vector2 splashCenter = critterLocalBounds.center;

            Vector3 splashPosition;
            splashPosition.x = critterPosition.x + splashCenter.x;
            splashPosition.y = inTank.Bounds.min.y + splashCenter.y / 4;
            splashPosition.z = critterPosition.z;

            ParticleSystem.ShapeModule particleEmitShape = m_UnderwaterParticles.shape;
            Vector3 shapeSize = particleEmitShape.scale;
            shapeSize.x = critterLocalBounds.width;
            shapeSize.y = critterLocalBounds.height / 4;
            //particleEmitShape.scale = shapeSize;

            ParticleSystem.EmitParams emit = default;
            emit.position = splashPosition;
            emit.applyShapeToPosition = true;
            m_UnderwaterParticles.Emit(emit, 48);

            Services.Audio.PostEvent("tank_water_splash_underwater");
        }

        #endregion // Splash

        #region Fill

        public IEnumerator RequestFill(SelectableTank inTank, float inDuration) {
            return inTank.WaterTransition.Replace(inTank, RequestFill_Routine(inTank, inDuration)).Wait();
        }

        private IEnumerator RequestFill_Routine(SelectableTank inTank, float inDuration) {
            AudioHandle pourAudio = Services.Audio.PostEvent("tank_water_pour").SetVolume(0).SetVolume(1, 0.1f);

            inTank.CurrentState |= TankState.Filling;

            m_FillParticles.transform.position = inTank.WaterCollider3D.bounds.center;
            m_FillParticles.Play();

            yield return 0.5f;

            m_FillParticleForceGroup.SetActive(true);

            yield return 1;

            m_FillParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            m_FillParticleForceGroup.SetActive(false);
            
            pourAudio.Stop(0.5f);
            yield return 0.5f;

            inTank.CurrentState &= ~TankState.Filling;

            yield return null;

            m_UnderwaterParticles.Stop();
        }

        #endregion // Fill
    }
}