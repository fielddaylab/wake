using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using UnityEngine;

namespace AquaAudio
{
    public class AudioTrigger : MonoBehaviour, ISceneManifestElement
    {
        [SerializeField] private string m_EventId = null;
        [SerializeField] private float m_CrossfadeDuration = 0;

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
                if (m_CrossfadeDuration > 0) {
                    m_Playback.SetVolume(0, 0).SetVolume(1, m_CrossfadeDuration);
                }
            }
        }

        private IEnumerator WaitToPlay()
        {
            while (Script.IsLoading)
            {
                yield return null;
            }

            m_Playback.Play();
            if (m_CrossfadeDuration > 0) {
                m_Playback.SetVolume(0, 0).SetVolume(1, m_CrossfadeDuration);
            }
        }

        private void OnDisable()
        {
            m_WaitRoutine.Stop();
            m_Playback.Stop(0.1f);
        }

        #if UNITY_EDITOR

        public void BuildManifest(SceneManifestBuilder builder) {
            AudioEvent.BuildManifestFromEventString(m_EventId, builder);
        }

        #endif // UNITY_EDITOR
    }
}