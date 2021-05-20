using System;
using Aqua.Option;
using AquaAudio;
using BeauUtil;
using BeauUtil.Services;
using Leaf;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(EventService), typeof(AudioMgr))]
    public partial class OptionsWatcher : ServiceBehaviour
    {
        protected override void Initialize()
        {
            base.Initialize();

            Services.Events.Register<OptionsData>(GameEvents.OptionsUpdated, ApplyOptions, this)
                .Register(GameEvents.ProfileLoaded, OnProfileLoaded, this);

            ApplyOptions(Services.Data.Options);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }

        private void OnProfileLoaded()
        {
            ApplyOptions(Services.Data.Options);
        }

        private void ApplyOptions(OptionsData inOptions)
        {
            AudioMgr audio = Services.Audio;

            inOptions.Audio.Master.Apply(ref audio.BusMix(AudioBusId.Master));
            inOptions.Audio.Music.Apply(ref audio.BusMix(AudioBusId.Music));
            inOptions.Audio.SFX.Apply(ref audio.BusMix(AudioBusId.SFX));
            inOptions.Audio.SFX.Apply(ref audio.BusMix(AudioBusId.Ambient));
        }
    }
}