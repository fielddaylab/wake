using System;
using Aqua.Animation;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProtoAqua.ExperimentV2
{
    public class ToggleableTankFeature : MonoBehaviour
    {
        #region Inspector

        public Collider Clickable;
        public PointerListener Pointer;
        public ParticleSystem Particles;
        public AmbientRenderer Light;

        #endregion // Inspector

        [NonSerialized] public bool State = false;
        [NonSerialized] public Action<bool> OnStateChanged;

        static public void RegisterHandlers(ToggleableTankFeature inFeature, Action<bool> inbOnChanged)
        {
            inFeature.Pointer.UserData = inFeature;
            inFeature.Pointer.onClick.AddListener(OnClicked);
        }

        static private void OnClicked(PointerEventData inEvent)
        {
            ToggleableTankFeature tank;
            PointerListener.TryGetUserData<ToggleableTankFeature>(inEvent, out tank);

            if (tank.State)
            {
                Disable(tank);
            }
            else
            {
                Enable(tank);
            }
        }

        static public void Enable(ToggleableTankFeature inFeature, bool inbForce = false)
        {
            if (!inbForce && inFeature.State)
                return;

            inFeature.State = true;
            inFeature.Particles.Play();
            inFeature.Light.enabled = true;
            inFeature.OnStateChanged?.Invoke(true);
            if (!inbForce)
                inFeature.Particles.Emit(32);
        }

        static public void Disable(ToggleableTankFeature inFeature, bool inbForce = false)
        {
            if (!inbForce && !inFeature.State)
                return;

            inFeature.State = false;
            inFeature.Particles.Stop();
            inFeature.Light.enabled = false;
            inFeature.OnStateChanged?.Invoke(false);
        }
    }
}