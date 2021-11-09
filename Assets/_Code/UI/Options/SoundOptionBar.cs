using UnityEngine;
using System;
using UnityEngine.UI;
using AquaAudio;
using BeauRoutine;

namespace Aqua.Option
{
    public class SoundOptionBar : MonoBehaviour 
    {
        #region Consts

        static private readonly TextId MasterLabel = "options.audio.master";
        static private readonly TextId MusicLabel = "options.audio.music";
        static private readonly TextId SFXLabel = "options.audio.sfx";
        static private readonly TextId VoiceLabel = "options.audio.voice";

        static private readonly TextId MuteLabel = "options.audio.mute";
        static private readonly TextId UnmuteLabel = "options.audio.unmute";

        #endregion // Consts

        #region Inspector

        [SerializeField] private LocText m_Label = null;
        
        [Header("Slider")]
        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private CursorInteractionHint m_SliderHint = null;
        [SerializeField] private RectTransform m_DefaultValue = null;
        
        [Header("Mute`")]
        [SerializeField] private Toggle m_MuteButton = null;
        [SerializeField] private CursorInteractionHint m_MuteHint = null;
        [SerializeField] private Sprite m_MuteIcon = null;
        [SerializeField] private Sprite m_UnmuteIcon = null;
        [SerializeField] private Image m_MuteIconDisplay = null;

        #endregion // Inspector

        [NonSerialized] private AudioBusId m_Id = AudioBusId.LENGTH;

        public Action<AudioBusId, OptionAudioBus> OnChanged;

        private void Awake()
        {
            m_Slider.onValueChanged.AddListener(b => VolumeChange(b));
            m_MuteButton.onValueChanged.AddListener(MuteChanged);
        }

        public void Initialize(AudioBusId inId, float inDefaultValue)
        {
            m_Id = inId;

            TextId label = GetLabel();
            m_Label.SetText(label);
            m_SliderHint.TooltipId = label;

            m_DefaultValue.SetAnchorX(inDefaultValue);
        }

        public void Sync(OptionAudioBus inBus)
        {
            m_MuteButton.SetIsOnWithoutNotify(inBus.Mute);
            m_Slider.SetValueWithoutNotify(inBus.Volume * m_Slider.maxValue);

            UpdateInteractable(inBus.Mute);
            UpdateMuteIcon(inBus.Mute);
        }

        #region Handlers

        private void MuteChanged(bool inbValue) 
        {
            OptionsData options = Save.Options;
            OptionAudioBus currentSettings = options.Audio[m_Id];

            currentSettings.Mute = inbValue;
            UpdateInteractable(inbValue);
            UpdateMuteIcon(inbValue);
            options.Audio[m_Id] = currentSettings;

            options.SetDirty();
            OnChanged?.Invoke(m_Id, currentSettings);

            Services.Events.QueueForDispatch(GameEvents.OptionsUpdated, options);
        }

        private void VolumeChange(float inVolume) 
        {
            OptionsData options = Save.Options;
            OptionAudioBus currentSettings = options.Audio[m_Id];
            
            currentSettings.Volume = inVolume / m_Slider.maxValue;
            options.Audio[m_Id] = currentSettings;

            options.SetDirty();
            OnChanged?.Invoke(m_Id, currentSettings);
            
            Services.Events.QueueForDispatch(GameEvents.OptionsUpdated, options);
        }

        #endregion // Handlers

        private void UpdateMuteIcon(bool inbMuted)
        {
            m_MuteButton.targetGraphic.color = !inbMuted ? AQColors.ContentBlue : AQColors.Teal;
            m_MuteIconDisplay.sprite = inbMuted ? m_MuteIcon : m_UnmuteIcon;
            m_MuteHint.TooltipId = inbMuted ? UnmuteLabel : MuteLabel;
        }

        private void UpdateInteractable(bool inbMuted)
        {
            m_Slider.interactable = !inbMuted;
            if (inbMuted)
            {
                m_Label.Graphic.SetAlpha(0.5f);
                m_Slider.GetComponent<CanvasGroup>().alpha = 0.5f;
            }
            else
            {
                m_Label.Graphic.SetAlpha(1);
                m_Slider.GetComponent<CanvasGroup>().alpha = 1;
            }
        }

        private TextId GetLabel() 
        {
            switch(m_Id) 
            {
                case AudioBusId.Master:
                    return MasterLabel;
                case AudioBusId.Music:
                    return MusicLabel;
                case AudioBusId.SFX:
                    return SFXLabel;
                case AudioBusId.Voice:
                    return VoiceLabel;
                default:
                    throw new Exception("No BusMix found for " + m_Id.ToString());
            }
        }
    }
}