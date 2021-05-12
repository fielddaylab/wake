using UnityEngine;
using System;
using Aqua;
using UnityEngine.UI;
using AquaAudio;
using TMPro;

namespace Aqua.Option
{
    public class SoundOptions : MonoBehaviour 
    {
        #region Inspector

        [SerializeField] public TextMeshProUGUI m_Text;
        [SerializeField] public Slider m_Slider;
        [SerializeField] public Button m_MuteButton;
        [SerializeField] public Image m_CheckBox;

        #endregion // Inspector

        [NonSerialized] private AudioBusId m_Id = AudioBusId.LENGTH;

        private void Awake()
        {
            m_Slider.onValueChanged.AddListener(b => VolumeChange(b));

            m_MuteButton.onClick.AddListener(Mute);
        }

        public void Initialize(AudioBusId inId, AudioPropertyBlock inBlock)
        {
            m_Id = inId;
            m_CheckBox.color = inBlock.Mute ? Color.gray : Color.white;
            m_Slider.SetValueWithoutNotify(inBlock.Volume);
            m_Text.SetText(GetText());
        }

        public void Reset() {
            m_MuteButton.onClick.RemoveAllListeners();
            m_Slider.onValueChanged.RemoveAllListeners();
        }

        private void Mute() 
        {
            m_CheckBox.color = m_CheckBox.color == Color.white ? Color.gray : Color.white;
            bool isMute = Services.Audio.BusMix(m_Id).Mute;
            Services.Data.Settings.UpdateAudioMute(m_Id, !isMute);
        }

        private void VolumeChange(float inVolume) 
        {
            Services.Data.Settings.UpdateAudioVolume(m_Id, inVolume);
        }

        private string GetText() 
        {
            switch(m_Id) 
            {
                case AudioBusId.Master:
                    return "Master";
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