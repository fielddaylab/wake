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
        [NonSerialized] private OptionsPerformance m_LastPerfOptions;

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
            inOptions.Audio.SFX.Apply(ref audio.BusMix(AudioBusId.Cinematic));
            inOptions.Audio.SFX.Apply(ref audio.BusMix(AudioBusId.Ambient));
            inOptions.Audio.Voice.Apply(ref audio.BusMix(AudioBusId.Voice));

            // TODO: Implement
            // AudioConfiguration audioConfig = AudioSettings.GetConfiguration();
            // if (Ref.Replace(ref audioConfig.speakerMode, inOptions.Audio.Mono ? AudioSpeakerMode.Mono : AudioSpeakerMode.Stereo))
            // {
            //     AudioSettings.Reset(audioConfig);
            // }

            QualitySettings.skinWeights = GetSkinning(inOptions.Performance.AnimationQuality);
            Application.targetFrameRate = GetFramerate(inOptions.Performance.Framerate);

            if (m_LastPerfOptions.AnimationQuality != inOptions.Performance.AnimationQuality) {
                GameQuality.OnAnimationChanged.Invoke(inOptions.Performance.AnimationQuality);
            }

            if (m_LastPerfOptions.EffectsQuality != inOptions.Performance.EffectsQuality) {
                GameQuality.OnEffectsChanged.Invoke(inOptions.Performance.EffectsQuality);
            }

            m_LastPerfOptions = inOptions.Performance;
        }

        static private int GetFramerate(OptionsPerformance.FramerateMode inFramerate)
        {
            #if UNITY_EDITOR
            return 60;
            #else
            return -1;
            #endif // UNITY_EDITOR
        }

        static private SkinWeights GetSkinning(OptionsPerformance.FeatureMode inAnimMode)
        {
            switch(inAnimMode) {
                case OptionsPerformance.FeatureMode.High: {
                    return SkinWeights.FourBones;
                }
                case OptionsPerformance.FeatureMode.Medium:
                default: {
                    return SkinWeights.TwoBones;
                }
                case OptionsPerformance.FeatureMode.Low: {
                    return SkinWeights.OneBone;
                }
            }
        }
    }

    static public class GameQuality {
        static public OptionsPerformance.FeatureMode Animation {
            get { return Save.Options.Performance.AnimationQuality; }
        }

        static public OptionsPerformance.FeatureMode Effects {
            get { return Save.Options.Performance.EffectsQuality; }
        }

        static public readonly CastableEvent<OptionsPerformance.FeatureMode> OnAnimationChanged = new CastableEvent<OptionsPerformance.FeatureMode>(8);

        static public readonly CastableEvent<OptionsPerformance.FeatureMode> OnEffectsChanged = new CastableEvent<OptionsPerformance.FeatureMode>(32);
    }
}