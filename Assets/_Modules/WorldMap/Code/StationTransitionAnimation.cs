using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.WorldMap
{
    public class StationTransitionAnimation : MonoBehaviour, IBaked
    {
        [Header("Ship Components")]
        public Transform ShipTransform;
        public ParticleSystem[] ShipParticles;
        public TrailRenderer[] ShipTrails;

        [Header("Animation")]
        public float ShipSpeed;
        public float LeftPos;
        public float RightPos;

        [Header("UI")]
        public CanvasGroup LoadingGroup;
        public LocText LoadingText;

        private void OnEnable() {
            ShipTransform.SetPosition(LeftPos, Axis.X);

            Script.OnSceneLoad(OnSceneLoad);
        }

        private void OnDisable() {
            if (!Services.Valid) {
                return;
            }

            Services.Events?.Dispatch(GameEvents.HotbarShow);
        }

        private void OnSceneLoad() {
            Services.Events.Dispatch(GameEvents.HotbarHide);
            for(int i = 0; i < 60; i += 10) {
                ShipTransform.Translate(i, 0, 0, Space.World);
                foreach(var ps in ShipParticles) {
                    ps.Emit(15);
                    ps.Simulate(5);
                }
            }

            foreach(var ps in ShipParticles) {
                ps.Play();
            }
            Services.Camera.SnapToTarget();

            LoadingText.SetTextFromString(Loc.Format("ui.loadingStation", Assets.Map(Save.Map.CurrentStationId()).ProperNameId()));
            LoadingGroup.alpha = 0;

            Routine.StartLoop(this, Tick);
            Routine.Start(this, ShowLoading());
        }

        private IEnumerator ShowLoading() {
            yield return 1;
            yield return LoadingGroup.Show(0.5f);
        }

        private void Tick() {
            if (ShipTransform.position.x < RightPos) {
                ShipTransform.Translate(ShipSpeed * Routine.DeltaTime, 0, 0, Space.World);
            }
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            ShipParticles = ShipTransform.GetComponentsInChildren<ParticleSystem>();
            ShipTrails = ShipTransform.GetComponentsInChildren<TrailRenderer>();
            return true;
        }

        #endif // UNITY_EDITOR
    }
}