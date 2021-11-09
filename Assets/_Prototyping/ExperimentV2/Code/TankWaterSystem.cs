using System;
using System.Collections;
using Aqua;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{

    public class TankWaterSystem : MonoBehaviour {
        private const float MaxWaterPitch = 1.6f;
        private const Curve WaterPitchCurve = Curve.CubeIn;

        private class ParticleListenerProxy : MonoBehaviour {
            public SelectableTank Tank;
            public Action<SelectableTank> Callback;

            private void OnParticleCollision(GameObject particles) {
                if (particles.layer == GameLayers.Water_Index) {
                    Callback(Tank);
                }
            }
        }

        #region Inspector

        [SerializeField, Required] private ParticleSystem m_FillParticles = null;
        [SerializeField, Required] private ParticleSystem m_FillImpactParticles = null;
        [SerializeField, Required] private ParticleSystem m_SplashSurfaceParticles = null;
        [SerializeField, Required] private ParticleSystem m_SplashDownParticles = null;
        [SerializeField, Required] private ParticleSystem m_RippleParticles = null;
        [SerializeField, Required] private ParticleSystem m_UnderwaterParticles = null;

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_WaterSFX;

        public void InitializeTank(SelectableTank inTank) {
            WorldUtils.ListenForLayerMask(inTank.WaterTrigger, GameLayers.Critter_Mask, (c) => OnWaterEnter(inTank, c), null);

            if (inTank.Type == TankType.Stress) {
                SetWaterHeight(inTank, 1);
            } else {
                SetWaterHeight(inTank, 0);
            }

            inTank.WaterSystem = this;

            var proxy = inTank.WaterCollider3D.gameObject.AddComponent<ParticleListenerProxy>();
            proxy.Tank = inTank;
            proxy.Callback = OnTankWaterFill;
        }

        public void SetActiveTank(SelectableTank inTank) {
            var downParticleTrigger = m_SplashDownParticles.trigger;
            downParticleTrigger.SetCollider(0, inTank.WaterCollider3D);
            
            var uwParticleTrigger = m_UnderwaterParticles.trigger;
            uwParticleTrigger.SetCollider(0, inTank.WaterCollider3D);

            var fillImpactParticleTrigger = m_FillImpactParticles.trigger;
            fillImpactParticleTrigger.SetCollider(0, inTank.WaterCollider3D);
        }

        private void OnWaterEnter(SelectableTank inTank, Collider2D inCreature) {
            ActorInstance actor = inCreature.GetComponentInParent<ActorInstance>();
            actor.InWater = true;

            switch(actor.Definition.Spawning.SpawnAnimation) {
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

        private void SurfaceSplash(SelectableTank inTank, ActorInstance inActor, Collider2D inCollider) {
            Vector3 critterPosition = inCollider.transform.position;
            Rect critterLocalBounds = inActor.Definition.LocalBoundsRect;
            float splashCenterX = critterLocalBounds.center.x;

            Vector3 splashPosition;
            splashPosition.x = critterPosition.x + splashCenterX;
            splashPosition.y = inTank.Bounds.max.y;
            splashPosition.z = critterPosition.z;

            ParticleSystem.ShapeModule surfaceShape = m_SplashSurfaceParticles.shape;
            Vector3 surfaceSize = surfaceShape.scale;
            surfaceSize.x = critterLocalBounds.width;
            surfaceShape.scale = surfaceSize;

            ParticleSystem.ShapeModule downShape = m_SplashDownParticles.shape;
            Vector3 downSize = surfaceShape.scale;
            downSize.x = critterLocalBounds.width;
            downShape.scale = downSize;

            ParticleSystem.EmitParams emit = default;
            emit.position = splashPosition;
            emit.applyShapeToPosition = true;

            m_SplashDownParticles.Emit(emit, 64);

            Routine.StartDelay(this, SurfaceSplashDelayed, emit, 0.1f);

            Services.Audio.PostEvent("tank_water_splash");
        }

        private void SurfaceSplashDelayed(ParticleSystem.EmitParams inParams) {
            m_SplashSurfaceParticles.Emit(inParams, 48);
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
            particleEmitShape.scale = shapeSize;

            ParticleSystem.EmitParams emit = default;
            emit.position = splashPosition;
            emit.applyShapeToPosition = true;
            m_UnderwaterParticles.Emit(emit, 48);

            Services.Audio.PostEvent("tank_water_splash_underwater");
        }

        private void OnTankWaterFill(SelectableTank inTank) {
            if ((inTank.CurrentState & TankState.Filling) != 0 && inTank.WaterFillProportion >= 1) {
                return;
            }

            float fillAmount = 0.5f * Routine.DeltaTime;
            float newHeightProportion = Math.Min(inTank.WaterFillProportion + fillAmount, 1);
            SetWaterHeight(inTank, newHeightProportion);
        }
    
        public IEnumerator RequestFill(SelectableTank inTank) {
            m_FillParticles.transform.SetPosition(inTank.transform.position.x, Axis.X, Space.World);
            m_FillParticles.Play(true);

            AudioHandle pourAudio = Services.Audio.PostEvent("tank_water_pour").SetVolume(0).SetVolume(1, 0.1f);

            inTank.CurrentState |= TankState.Filling;

            while(inTank.WaterFillProportion < 1) {
                yield return null;
            }

            m_FillParticles.Stop();
            inTank.CurrentState &= ~TankState.Filling;

            pourAudio.Stop(0.1f);

            while(m_FillParticles.particleCount > 0) {
                yield return null;
            }
        }

        static public void SetWaterHeight(SelectableTank inTank, float inProportion) {
            inTank.WaterFillProportion = inProportion;

            Rect originalRect = inTank.WaterRect;
            Vector2 originalCenter = originalRect.center;
            
            Rect newRect = originalRect;
            Curve evalCurve;
            if ((inTank.CurrentState & TankState.Draining) != 0) {
                evalCurve = Curve.Smooth;
            } else {
                evalCurve = Curve.QuadOut;
            }
            newRect.height *= TweenUtil.Evaluate(evalCurve, inProportion);
            
            Vector2 newCenter = newRect.center;
            float newHeight = newRect.height;

            Vector3 collider3dCenter = inTank.WaterCollider3D.center;
            collider3dCenter.y = newCenter.y - originalCenter.y;
            inTank.WaterCollider3D.center = collider3dCenter;

            Vector3 collider3dSize = inTank.WaterCollider3D.size;
            collider3dSize.y = Math.Max(newHeight, 0.02f);
            inTank.WaterCollider3D.size = collider3dSize;

            Vector2 colliderCenter = inTank.WaterTrigger.offset;
            colliderCenter.y = newCenter.y - originalCenter.y;
            inTank.WaterTrigger.offset = colliderCenter;

            inTank.WaterTrigger.size = newRect.size;

            var particleShape = inTank.WaterAmbientParticles.shape;
            Vector3 shapeScale = particleShape.scale;
            shapeScale.x = newRect.width;
            shapeScale.y = newRect.height;
            Vector3 shapePos = particleShape.position;
            shapePos.y = newCenter.y - originalCenter.y;
            particleShape.position = shapePos;
            particleShape.scale = shapeScale;

            var particleEmission = inTank.WaterAmbientParticles.emission;
            particleEmission.enabled = inProportion > 0;

            var drainShape = inTank.WaterDrainParticles.shape;
            drainShape.length = newHeight * 0.5f;
            var drainEmission = inTank.WaterDrainParticles.emission;
            drainEmission.enabled = inProportion > 0.2f;

            if (inProportion > 0 && (inTank.CurrentState & TankState.Selected) != 0) {
                if (!inTank.WaterAudioLoop.IsPlaying()) {
                    inTank.WaterAudioLoop = Services.Audio.PostEvent("tank_water_loop");
                }
                inTank.WaterAudioLoop.SetVolume(inProportion);
                inTank.WaterAudioLoop.SetPitch(Mathf.Lerp(MaxWaterPitch, 1, WaterPitchCurve.Evaluate(inProportion)));
            } else {
                inTank.WaterAudioLoop.Stop();
            }

            inTank.WaterRenderer.SetPosition(newCenter, Axis.Y, Space.Self);
            inTank.WaterRenderer.SetScale(newHeight, Axis.Y);
        }
    
        public IEnumerator DrainWaterOverTime(SelectableTank inTank, float inDuration) {
            var audio = Services.Audio.PostEvent("tank_water_drain");
            try {
                inTank.CurrentState |= TankState.Draining;
                inTank.WaterDrainParticles.Play();
                m_RippleParticles.Clear();
                yield return Tween.Float(inTank.WaterFillProportion, 0, (f) => SetWaterHeight(inTank, f), inDuration * inTank.WaterFillProportion)
                    .OnUpdate((f) => audio.SetPitch(Mathf.Lerp(MaxWaterPitch, 1, WaterPitchCurve.Evaluate(f))));
                inTank.CurrentState &= ~TankState.Draining;
                inTank.WaterDrainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            finally {
                inTank.WaterDrainParticles.Stop();
                audio.Stop(0);
            }
        }
    }
}