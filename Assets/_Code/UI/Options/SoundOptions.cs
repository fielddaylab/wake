using UnityEngine;
using System;
using Aqua;
using UnityEngine.UI;
using AquaAudio;
using TMPro;
using BeauRoutine;

namespace Aqua.Option
{
    public class SoundOptions : MonoBehaviour 
    {
        #region Inspector

        [SerializeField] private LocText m_Text = null;
        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private Toggle m_MuteButton = null;

        #endregion // Inspector

        [NonSerialized] private AudioBusId m_Id = AudioBusId.LENGTH;

        private void Awake()
        {
            m_Slider.onValueChanged.AddListener(b => VolumeChange(b));
            m_MuteButton.onValueChanged.AddListener(MuteChanged);
        }

        public void Initialize(AudioBusId inId, OptionAudioBus inBus)
        {
            m_Id = inId;
            m_MuteButton.SetIsOnWithoutNotify(inBus.Mute);
            m_Slider.SetValueWithoutNotify(inBus.Volume * m_Slider.maxValue);
            m_Text.SetText(GetLabel());

            UpdateInteractable(inBus.Mute);
        }

        private void MuteChanged(bool inbValue) 
        {
            OptionsData options = Services.Data.Options;
            OptionAudioBus currentSettings = options.Audio[m_Id];

            currentSettings.Mute = inbValue;
            UpdateInteractable(inbValue);
            options.Audio[m_Id] = currentSettings;

            options.SetDirty();
            Services.Events.Dispatch(GameEvents.OptionsUpdated, options);
        }

        private void VolumeChange(float inVolume) 
        {
            OptionsData options = Services.Data.Options;
            OptionAudioBus currentSettings = options.Audio[m_Id];
            
            currentSettings.Volume = inVolume / m_Slider.maxValue;
            options.Audio[m_Id] = currentSettings;

            options.SetDirty();
            Services.Events.Dispatch(GameEvents.OptionsUpdated, options);
        }

        private void UpdateInteractable(bool inbMuted)
        {
            m_Slider.interactable = !inbMuted;
            if (inbMuted)
            {
                m_Text.Graphic.SetAlpha(0.5f);
                m_Slider.GetComponent<CanvasGroup>().alpha = 0.5f;
            }
            else
            {
                m_Text.Graphic.SetAlpha(1);
                m_Slider.GetComponent<CanvasGroup>().alpha = 1;
            }
        }

        private string GetLabel() 
        {
            switch(m_Id) 
            {
                case AudioBusId.Master:
                    return "Audio";
                case AudioBusId.Music:
                    return "Music";
                case AudioBusId.SFX:
                    return "SFX";
                default:
                    throw new Exception("No BusMix found for " + m_Id.ToString());
            }
        }
    }
}