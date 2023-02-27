using System;
using System.Collections;
using Aqua;
using Aqua.Scripting;
using BeauRoutine;
using Leaf.Runtime;
using UnityEngine;

namespace AquaAudio
{
    public class AudioTrigger : ScriptComponent, ISceneManifestElement
    {
        [SerializeField] private string m_EventId = null;
        [SerializeField] private float m_CrossfadeDuration = 0;
        [SerializeField] private float m_FadeOutDuration = 0.1f;
        [SerializeField] private bool m_PlayOnAwake = true;
        [SerializeField] private Transform m_Location = null;

        private Routine m_WaitRoutine;
        private AudioHandle m_Playback;

        private void OnEnable()
        {
            Async.InvokeAsync(BeginLoading);
        }

        private void BeginLoading()
        {
            if (!this || !isActiveAndEnabled || string.IsNullOrEmpty(m_EventId))
                return;

            if (Script.IsLoading)
            {
                m_Playback = Services.Audio.PostEvent(m_EventId, AudioPlaybackFlags.PreloadOnly).TrackPosition(m_Location);
                if (m_PlayOnAwake)
                {
                    m_WaitRoutine = Routine.Start(this, WaitToPlay());
                }
            }
            else
            {
                if (m_PlayOnAwake)
                {
                    m_Playback = Services.Audio.PostEvent(m_EventId).TrackPosition(m_Location);
                    if (m_CrossfadeDuration > 0)
                    {
                        m_Playback.SetVolume(0, 0).SetVolume(1, m_CrossfadeDuration);
                    }
                }
                else
                {
                    m_Playback = Services.Audio.PostEvent(m_EventId, AudioPlaybackFlags.PreloadOnly).TrackPosition(m_Location);
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
            if (m_CrossfadeDuration > 0)
            {
                m_Playback.SetVolume(0, 0).SetVolume(1, m_CrossfadeDuration);
            }
        }

        [LeafMember("PlayAudio")]
        public void Play()
        {
            if (Script.IsLoading)
            {
                m_WaitRoutine.Replace(this, WaitToPlay());
            }
            else
            {
                if (m_Playback.Exists())
                {
                    m_Playback.Play();
                }
                else
                {
                    m_Playback = Services.Audio.PostEvent(m_EventId).TrackPosition(m_Location);
                    if (m_CrossfadeDuration > 0)
                    {
                        m_Playback.SetVolume(0, 0).SetVolume(1, m_CrossfadeDuration);
                    }
                }
            }
        }

        private void OnDisable()
        {
            m_WaitRoutine.Stop();
            m_Playback.Stop(m_FadeOutDuration);
        }

#if UNITY_EDITOR

        public void BuildManifest(SceneManifestBuilder builder)
        {
            AudioEvent.BuildManifestFromEventString(m_EventId, builder);
        }

#endif // UNITY_EDITOR
    }
}