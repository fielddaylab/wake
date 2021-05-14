using UnityEngine;
using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using Aqua;
using AquaAudio;

namespace Aqua.Option
{
    public class Options : ISerializedObject, ISerializedVersion
    {
        private OptionAudio optionAudio;

        public ushort Version => throw new NotImplementedException();

        public Options()
        {
            optionAudio = new OptionAudio();
            InitializeAudio();

        }

        public void LoadOptions() 
        {
            LoadAudio();

        }

        private void InitializeAudio() 
        {
            optionAudio.Master = Services.Audio.BusMix(AudioBusId.Master);
            optionAudio.Music = Services.Audio.BusMix(AudioBusId.Music);
            optionAudio.SFX = Services.Audio.BusMix(AudioBusId.SFX);
        }

        #region Audio

        public void LoadAudio() 
        {
            foreach(var source in AudioSettings.Sources()) 
            {
                AudioPropertyBlock _Block = GetBusMix(source);
                Services.Audio.BusMix(source).Mute = _Block.Mute;
                Services.Audio.BusMix(source).Volume = _Block.Volume;
            }
        }

        public void LoadAudio(AudioBusId m_Id)
        {
            AudioPropertyBlock _Block = GetBusMix(m_Id);
            Services.Audio.BusMix(m_Id).Mute = _Block.Mute;
            Services.Audio.BusMix(m_Id).Volume = _Block.Volume;
        }

        public void UpdateAudioMute(AudioBusId m_Id, bool inMute) 
        {
            AudioPropertyBlock _Block = GetBusMix(m_Id);
            _Block.Mute = inMute;
            AssignBlock(_Block, m_Id);
            LoadAudio(m_Id);

        }

        public void UpdateAudioVolume(AudioBusId m_Id, float inVolume) 
        {
            AudioPropertyBlock _Block = GetBusMix(m_Id);
            _Block.Volume = inVolume;
            AssignBlock(_Block, m_Id);
            LoadAudio(m_Id);

        }


        public void Serialize(Serializer ioSerializer)
        {
            throw new NotImplementedException();
        }

        private void AssignBlock(AudioPropertyBlock inBlock, AudioBusId inBusId) 
        {
            switch(inBusId)
            {
                case AudioBusId.Master:
                    optionAudio.Master = inBlock;
                    break;
                case AudioBusId.Music:
                    optionAudio.Music = inBlock;
                    break;
                case AudioBusId.SFX:
                    optionAudio.SFX = inBlock;
                    break;
                default:
                    throw new Exception("Cannot find busMix");
            }
        }

        public AudioPropertyBlock GetBusMix(AudioBusId busId) 
        {
            switch(busId)
            {
                case AudioBusId.Master:
                    return optionAudio.Master;
                case AudioBusId.Music:
                    return optionAudio.Music;
                case AudioBusId.SFX:
                    return optionAudio.SFX;
                default:
                    return optionAudio.Master;
            }
        }
    }

    #endregion // Audio

}