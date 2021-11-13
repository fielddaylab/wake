using System;
using Aqua.Option;
using AquaAudio;
using BeauUtil;
using BeauUtil.Services;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(EventService), typeof(AudioMgr))]
    internal partial class OptionsWatcher : ServiceBehaviour
    {
        protected override void Initialize()
        {
            base.Initialize();

            Services.Events.Register<OptionsData>(GameEvents.OptionsUpdated, ApplyOptions, this)
                .Register(GameEvents.ProfileLoaded, OnProfileLoaded, this);

            ApplyOptions(Save.Options);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
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

            Application.targetFrameRate = GetFramerate(inOptions.Performance.Framerate);
        }

        static private int GetFramerate(OptionsPerformance.FramerateMode inFramerate)
        {
            switch(inFramerate)
            {
                case OptionsPerformance.FramerateMode.Stable:
                    return 30;

                case OptionsPerformance.FramerateMode.High:
                    return 60;

                default:
                    return -1;
            }
        }
    }
}