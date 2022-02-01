using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using UnityEngine;

namespace AquaAudio
{
    public class AudioTrigger : MonoBehaviour
    {
        [SerializeField] private string m_EventId = null;

        private Routine m_WaitRoutine;
        private AudioHandle m_Playback;

        private void OnEnable()
        {
            if (Script.IsLoading)
            {
                m_Playback = Services.Audio.PostEvent(m_EventId, AudioPlaybackFlags.PreloadOnly);
                m_WaitRoutine = Routine.Start(this, WaitToPlay());
            }
            else
            {
                m_Playback = Services.Audio.PostEvent(m_EventId);
            }
        }

        private IEnumerator WaitToPlay()
        {
            while (Script.IsLoading)
            {
                yield return null;
            }

            m_Playback.Play();
        }

        private void OnDisable()
        {
            m_WaitRoutine.Stop();
            m_Playback.Stop(0.1f);
        }
    }
}