using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class AudioPanel : OptionsMenu.Panel
    {
        #region Inspector

        [SerializeField] private SoundOptionBar m_MasterBus = null;
        
        [Header("SubBusses")]
        [SerializeField] private CanvasGroup m_SubBusGroup = null;
        [SerializeField] private SoundOptionBar m_MusicBus = null;
        [SerializeField] private SoundOptionBar m_SFXBus = null;
        [SerializeField] private SoundOptionBar m_VoiceBus = null;

        #endregion // Inspector

        private void Awake()
        {
            m_MasterBus.OnChanged = OnMasterBusChanged;

            m_MasterBus.Initialize(AudioBusId.Master, 1);
            m_MusicBus.Initialize(AudioBusId.Music, 0.8f);
            m_SFXBus.Initialize(AudioBusId.SFX, 0.8f);
            m_VoiceBus.Initialize(AudioBusId.Voice, 0.8f);
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);
            
            m_MasterBus.Sync(inOptions.Audio.Master);
            m_MusicBus.Sync(inOptions.Audio.Music);
            m_SFXBus.Sync(inOptions.Audio.SFX);
            m_VoiceBus.Sync(inOptions.Audio.Voice);

            OnMasterBusChanged(AudioBusId.Master, inOptions.Audio.Master);
        }

        private void OnMasterBusChanged(AudioBusId inBusId, OptionAudioBus inBus)
        {
            if (inBus.Mute)
            {
                m_SubBusGroup.interactable = false;
                m_SubBusGroup.alpha = 0.5f;
            }
            else
            {
                m_SubBusGroup.interactable = true;
                m_SubBusGroup.alpha = 1;
            }
        }
    }
}