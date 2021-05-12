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

        public void Initialize(AudioBusId inId) 
        {
            m_Id = inId;

            m_Slider?.SetValueWithoutNotify(Services.Audio.BusMix(m_Id).Volume);
            m_MuteButton.onClick.AddListener(Mute);
            m_CheckBox.color = Color.white;
            m_Text.SetText(GetText());
        }

        public void Mute() 
        {
            m_CheckBox.color = m_CheckBox.color == Color.white ? Color.gray : Color.white;
            bool isMute = Services.Audio.BusMix(m_Id).Mute;
            Services.Audio.BusMix(m_Id).Mute = !isMute;
        }

        private string GetText() 
        {
            switch(m_Id) 
            {
                case AudioBusId.Master:
                    return "Master";
                case AudioBusId.Music:
                    return "Music";
                default:
                    return "SFX";
            }
        }




    }
}