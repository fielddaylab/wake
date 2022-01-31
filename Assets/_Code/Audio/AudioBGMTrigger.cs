using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using UnityEngine;

namespace AquaAudio
{
    public class AudioBGMTrigger : MonoBehaviour
    {
        [SerializeField] private string m_EventId = null;
        [SerializeField] private float m_Crossfade = 0.5f;
        [SerializeField] private bool m_StopOnDisable = true;

        private Routine m_WaitRoutine;
        private AudioHandle m_BGM;

        private void OnEnable()
        {
            if (Services.Audio.CurrentMusic().EventId() == m_EventId)
            {
                return;
            }

            if (Services.State.IsLoadingScene())
            {
                m_BGM = Services.Audio.PostEvent(m_EventId, AudioPlaybackFlags.PreloadOnly);
                m_WaitRoutine = Routine.Start(this, WaitToPlay());
            }
            else
            {
                m_BGM = Services.Audio.SetMusic(m_EventId, m_Crossfade);
            }
        }

        private IEnumerator WaitToPlay()
        {
            while (Services.State.IsLoadingScene())
            {
                yield return null;
            }

            Services.Audio.SetMusic(m_BGM, m_Crossfade);
        }

        private void OnDisable()
        {
            m_WaitRoutine.Stop();

            if (!m_StopOnDisable)
                return;
            
            if (Services.Audio)
            {
                if (m_BGM.Exists() && Services.Audio.CurrentMusic().EventId() == m_EventId)
                {
                    m_BGM = default(AudioHandle);
                    Services.Audio.SetMusic(null, m_Crossfade);
                }
            }
        }
    }
}