using System;
using Aqua.Option;
using AquaAudio;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using Leaf;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(EventService), typeof(AudioMgr))]
    internal partial class OptionsWatcher : ServiceBehaviour
    {
        public float LowCameraResHeight = 660f;
        
        private PerformanceTracker m_PerfTracker;

        protected override void Initialize()
        {
            base.Initialize();

            Services.Events.Register<OptionsData>(GameEvents.OptionsUpdated, ApplyOptions, this)
                .Register(GameEvents.ProfileLoaded, OnProfileLoaded, this);

            ApplyOptions(Save.Options);

            m_PerfTracker = new PerformanceTracker(256);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }

        private void LateUpdate()
        {

        }

        private void OnProfileLoaded()
        {
            ApplyOptions(Save.Options);
        }

        private void ApplyOptions(OptionsData inOptions)
        {
            AudioMgr audio = Services.Audio;

            inOptions.Audio.Master.Apply(ref audio.BusMix(AudioBusId.Master));
            inOptions.Audio.Music.Apply(ref audio.BusMix(AudioBusId.Music));
            inOptions.Audio.SFX.Apply(ref audio.BusMix(AudioBusId.SFX));
            inOptions.Audio.SFX.Apply(ref audio.BusMix(AudioBusId.Ambient));
            inOptions.Audio.Voice.Apply(ref audio.BusMix(AudioBusId.Voice));

            // TODO: Implement
            // AudioConfiguration audioConfig = AudioSettings.GetConfiguration();
            // if (Ref.Replace(ref audioConfig.speakerMode, inOptions.Audio.Mono ? AudioSpeakerMode.Mono : AudioSpeakerMode.Stereo))
            // {
            //     AudioSettings.Reset(audioConfig);
            // }

            // if (inOptions.Performance.Resolution == OptionsPerformance.ResolutionMode.Low) {
            //     QualitySettings.resolutionScalingFixedDPIFactor
            // }
        }
    }
}