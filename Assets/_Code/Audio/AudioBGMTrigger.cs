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
        [SerializeField] private float m_Crossfade = 0;

        private Routine m_WaitRoutine;
        private AudioHandle m_BGM;

        private void OnEnable()
        {
            if (Services.State.IsLoadingScene())
            {
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

            m_BGM = Services.Audio.SetMusic(m_EventId, m_Crossfade);
        }

        private void OnDisable()
        {
            m_WaitRoutine.Stop();
            if (m_BGM.Exists())
            {
                m_BGM = default(AudioHandle);
                Services.Audio.SetMusic(null, m_Crossfade);
            }
        }
    }
}