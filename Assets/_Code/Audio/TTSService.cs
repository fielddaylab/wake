using System;
using Aqua;
using BeauUtil.Services;
using Aqua.Option;

namespace AquaAudio
{
    [ServiceDependency(typeof(DataService), typeof(EventService))]
    public class TTSService : ServiceBehaviour
    {
        private enum Priority
        {
            Empty,

            Tooltip,
            Text
        }

        [NonSerialized] private bool m_PlaybackEnabled = false;
        [NonSerialized] private OptionsAccessibility.TTSMode m_Mode;

        [NonSerialized] private Priority m_CurrentPriority = Priority.Empty;

        public void Tooltip(TextId inTextId)
        {
            if (inTextId.IsEmpty)
                return;

            if (!CheckMode(OptionsAccessibility.TTSMode.Tooltips))
                return;

            if (!CheckPriority(Priority.Tooltip))
                return;

            Speak(Priority.Tooltip, Loc.Find(inTextId), 1);
        }

        public void Tooltip(string inText)
        {
            if (string.IsNullOrEmpty(inText))
                return;

            if (!CheckMode(OptionsAccessibility.TTSMode.Tooltips))
                return;

            if (!CheckPriority(Priority.Tooltip))
                return;

            Speak(Priority.Tooltip, inText, 1);
        }

        public void Text(string inText)
        {
            if (string.IsNullOrEmpty(inText))
                return;

            if (!CheckMode(OptionsAccessibility.TTSMode.Full))
                return;

            if (!CheckPriority(Priority.Text))
                return;

            Speak(Priority.Text, inText, 1);
        }

        public void Text(string inText, float inPitch)
        {
            if (string.IsNullOrEmpty(inText))
                return;

            if (!CheckMode(OptionsAccessibility.TTSMode.Full))
                return;

            if (!CheckPriority(Priority.Text))
                return;

            Speak(Priority.Text, inText, inPitch);
        }

        private bool CheckMode(OptionsAccessibility.TTSMode inMode)
        {
            return m_PlaybackEnabled && m_Mode >= inMode;
        }

        private bool CheckPriority(Priority inNewPriority)
        {
            CheckFinished();
            return inNewPriority >= m_CurrentPriority;
        }

        private void Speak(Priority inNewPriority, string inText, float inPitch)
        {
            m_CurrentPriority = inNewPriority;
            TTS.Speak(inText, inPitch);
        }

        public void Cancel()
        {
            if (m_CurrentPriority > 0)
            {
                m_CurrentPriority = Priority.Empty;
                TTS.Cancel();
            }
        }

        private void CheckFinished()
        {
            if (m_CurrentPriority > 0 && !TTS.IsSpeaking())
                m_CurrentPriority = 0;
        }

        #region Handlers

        private void OnOptionsChanged()
        {
            OptionsData options = Save.Options;
            OptionsAccessibility accessibility = options.Accessibility;
            m_Mode = accessibility.TTS;
            TTS.Rate = accessibility.TTSRate;

            float volume = options.Audio.Master.Volume * options.Audio.Voice.Volume;
            bool mute = options.Audio.Master.Mute || options.Audio.Voice.Mute;

            TTS.Volume = volume;
            m_PlaybackEnabled = m_Mode > 0 && !mute;
            m_PlaybackEnabled |= TTS.IsAvailable();

            if (!m_PlaybackEnabled)
            {
                Cancel();
            }
        }

        #endregion // Handlers

        #region IService

        protected override void Initialize()
        {
            base.Initialize();

            Services.Events.Register(GameEvents.OptionsUpdated, OnOptionsChanged, this)
                .Register(GameEvents.ProfileLoaded, OnOptionsChanged, this)
                .Register(GameEvents.SceneWillUnload, Cancel, this);

            OnOptionsChanged();
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }

        #endregion // IService
    }
}