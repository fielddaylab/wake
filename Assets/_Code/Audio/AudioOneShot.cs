using System;
using System.Collections;
using Aqua;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using Leaf.Runtime;
using UnityEngine;

namespace AquaAudio
{
    public class AudioOneShot : ScriptComponent, ISceneManifestElement
    {
        [SerializeField] private SerializedHash32 m_EventId = null;
        [SerializeField] private Transform m_Location = null;

        [NonSerialized] private AudioHandle m_Playback;

        public float Volume = 1;

        [LeafMember("PlayAudio")]
        public void Play()
        {
            m_Playback = Services.Audio.PostEvent(m_EventId).TrackPosition(m_Location).SetVolume(Volume);
        }

        private void OnDisable()
        {
            m_Playback.Stop();
        }

#if UNITY_EDITOR

        public void BuildManifest(SceneManifestBuilder builder)
        {
            AudioEvent.BuildManifestFromEventString(m_EventId.Source(), builder);
        }

#endif // UNITY_EDITOR
    }
}