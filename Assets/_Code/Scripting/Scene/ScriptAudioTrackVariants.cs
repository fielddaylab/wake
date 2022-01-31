using System;
using System.Collections;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Aqua.Scripting {
    public class ScriptAudioTrackVariants : ScriptComponent {
        #region Types

        [Serializable]
        private class Layer {
            public SerializedHash32 Id;
            public string EventId;
            public uint Priority = 1;
            
            [Header("Collision Triggers")]
            public Collider2D PlayerTrigger;
            public bool Persistent;
            [NonSerialized] public TriggerListener2D PlayerListener;

            [NonSerialized] public AudioHandle Audio;
            [NonSerialized] public bool Enabled;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private string m_BaseEventId = null;
        [SerializeField] private Layer[] m_Layers = null;
        [SerializeField] private float m_CrossFadeDuration = 5;
        [SerializeField] private float m_FadeOutDuration = 0.5f;

        #endregion // Inspector

        private AudioHandle m_BaseAudio;
        private Routine m_WaitHandle;
        private Routine m_ThinkUpdate;

        private AudioHandle m_CurrentAudio;

        private void OnEnable() {
            m_BaseAudio = Services.Audio.PostEvent(m_BaseEventId, AudioPlaybackFlags.PreloadOnly);
            m_BaseAudio.SetVolume(0);
            foreach (var layer in m_Layers) {
                layer.Audio = Services.Audio.PostEvent(layer.EventId, AudioPlaybackFlags.PreloadOnly);
                layer.Audio.SetVolume(0);
                if (layer.PlayerTrigger && !layer.PlayerListener) {
                    Layer cachedLayer = layer;
                    layer.PlayerListener = WorldUtils.ListenForPlayer(layer.PlayerTrigger,
                        (c) => OnPlayerEnterLayer(cachedLayer),
                        (c) => OnPlayerExitLayer(cachedLayer));
                }
            }

            m_ThinkUpdate = Routine.StartLoop(this, PauseHiddenTracks).SetPhase(RoutinePhase.ThinkUpdate);

            if (Services.State.IsLoadingScene()) {
                m_WaitHandle = Routine.Start(this, WaitToPlay());
            } else {
                UpdatePlayingTrack();
            }
        }

        private IEnumerator WaitToPlay() {
            while(Services.State.IsLoadingScene()) {
                yield return null;
            }
            yield return null;

            UpdatePlayingTrack();
        }

        private void UpdatePlayingTrack() {
            AudioHandle desiredAudio = m_BaseAudio;
            uint desiredPriority = 0;
            StringHash32 desiredAudioId = m_BaseEventId;

            foreach(var layer in m_Layers) {
                if (layer.Enabled && layer.Priority > desiredPriority) {
                    desiredPriority = layer.Priority;
                    desiredAudio = layer.Audio;
                    desiredAudioId = layer.Id.IsEmpty ? layer.EventId : layer.Id;
                }
            }

            if (m_CurrentAudio == desiredAudio) {
                return;
            }

            Log.Msg("[ScriptAudioTrackVariants] Switching tracks to {0}", desiredAudioId);

            m_CurrentAudio.SetVolume(0, m_CrossFadeDuration, Curve.CubeOut);
            desiredAudio.SetVolume(1, m_CrossFadeDuration, Curve.CubeIn);
            if (desiredAudio.IsPaused()) {
                desiredAudio.Resume();
            } else if (!desiredAudio.IsPlaying()) {
                desiredAudio.Play();
            }
            AudioHandle.Sync(m_CurrentAudio, desiredAudio);
            m_CurrentAudio = desiredAudio;
        }

        private void PauseHiddenTracks() {
            CheckPauseTrack(m_BaseAudio);
            foreach(var layer in m_Layers) {
                CheckPauseTrack(layer.Audio);
            }
        }

        private void CheckPauseTrack(AudioHandle audio) {
            if (audio.IsPlaying() && audio.GetVolume() == 0) {
                audio.Pause();
            }
        }

        private void OnDisable() {
            m_WaitHandle.Stop();
            m_ThinkUpdate.Stop();

            if (Services.Audio) {
                m_BaseAudio.Stop(m_FadeOutDuration);
                foreach (var layer in m_Layers) {
                    layer.Audio.Stop(m_FadeOutDuration);
                    if (layer.PlayerListener) {
                        layer.PlayerListener.enabled = false;
                    }
                    layer.Enabled = false;
                }
            }
        }

        private void OnPlayerEnterLayer(Layer layer) {
            if (!isActiveAndEnabled)
                return;

            layer.Enabled = true;
            UpdatePlayingTrack();
        }

        private void OnPlayerExitLayer(Layer layer) {
            if (!isActiveAndEnabled || layer.Persistent)
                return;

            layer.Enabled = false;
            UpdatePlayingTrack();
        }
    }
}