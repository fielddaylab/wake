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
    [AddComponentMenu("Aqualab/Scripting/Script Audio Track Variants")]
    public class ScriptAudioTrackVariants : ScriptComponent {
        #region Types

        private enum Mode {
            Single,
            Multi
        }

        [Serializable]
        private class Layer {
            public SerializedHash32 Id;
            public string EventId;
            public uint Priority = 1;
            
            [Header("Collision Triggers")]
            public Collider2D PlayerTrigger;
            public bool Persistent;
            [NonSerialized] public TriggerListener2D PlayerListener;

            [NonSerialized] public bool Enabled;
            [NonSerialized] public AudioGroup Group;
        }

        private struct AudioGroup {
            public AudioHandle Handle;
            public bool AppliedState;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private string m_BaseEventId = null;
        [SerializeField] private Layer[] m_Layers = null;
        [SerializeField] private float m_CrossFadeDuration = 5;
        [SerializeField] private float m_FadeOutDuration = 0.5f;
        [SerializeField] private bool m_SyncLayers = true;
        [SerializeField] private Mode m_Mode = Mode.Single;
        [SerializeField] private Curve m_FadeOutCurve = Curve.CubeOut;
        [SerializeField] private Curve m_FadeInCurve = Curve.CubeIn;

        #endregion // Inspector

        private AudioGroup m_BaseAudio;
        private Routine m_WaitHandle;
        private Routine m_ThinkUpdate;

        private AudioHandle m_CurrentSingleAudio;

        #region Unity Events

        private void OnEnable() {
            m_BaseAudio.Handle = Services.Audio.PostEvent(m_BaseEventId, AudioPlaybackFlags.PreloadOnly);
            m_BaseAudio.Handle.SetVolume(0);
            foreach (var layer in m_Layers) {
                layer.Group.Handle = Services.Audio.PostEvent(layer.EventId, AudioPlaybackFlags.PreloadOnly);
                layer.Group.Handle.SetVolume(0);
                if (layer.PlayerTrigger && !layer.PlayerListener) {
                    Layer cachedLayer = layer;
                    layer.PlayerListener = WorldUtils.ListenForPlayer(layer.PlayerTrigger,
                        (c) => OnPlayerEnterLayer(cachedLayer),
                        (c) => OnPlayerExitLayer(cachedLayer));
                }
            }

            m_ThinkUpdate = Routine.StartLoop(this, PauseHiddenTracks).SetPhase(RoutinePhase.ThinkUpdate);

            if (Script.IsLoading) {
                m_WaitHandle = Routine.Start(this, WaitToPlay());
            } else {
                UpdatePlayingTracks();
            }
        }

        private void OnDisable() {
            m_WaitHandle.Stop();
            m_ThinkUpdate.Stop();

            if (Services.Audio) {
                m_BaseAudio.Handle.Stop(m_FadeOutDuration);
                foreach (var layer in m_Layers) {
                    layer.Group.Handle.Stop(m_FadeOutDuration);
                    if (layer.PlayerListener) {
                        layer.PlayerListener.enabled = false;
                    }
                    layer.Enabled = false;
                }
            }
        }

        #endregion // Unity Events

        #region Playback

        private IEnumerator WaitToPlay() {
            while(Script.IsLoading) {
                yield return null;
            }
            yield return null;

            UpdatePlayingTracks();
        }

        private void UpdatePlayingTracks() {
            switch(m_Mode) {
                case Mode.Single: {
                    UpdateSingleTrackMode();
                    break;
                }
                case Mode.Multi: {
                    UpdateMultiTrackMode();
                    break;
                }
            }
        }

        private void UpdateSingleTrackMode() {
            AudioHandle desiredAudio = m_BaseAudio.Handle;
            uint desiredPriority = 0;
            StringHash32 desiredAudioId = m_BaseEventId;

            foreach(var layer in m_Layers) {
                if (layer.Enabled && layer.Priority > desiredPriority) {
                    desiredPriority = layer.Priority;
                    desiredAudio = layer.Group.Handle;
                    desiredAudioId = layer.Id.IsEmpty ? layer.EventId : layer.Id;
                }
            }

            if (m_CurrentSingleAudio == desiredAudio) {
                return;
            }

            Log.Msg("[ScriptAudioTrackVariants] Switching tracks to {0}", desiredAudioId);

            m_CurrentSingleAudio.SetVolume(0, m_CrossFadeDuration, m_FadeOutCurve);
            desiredAudio.SetVolume(1, m_CrossFadeDuration, m_FadeInCurve);
            if (desiredAudio.IsPaused()) {
                desiredAudio.Resume();
            } else if (!desiredAudio.IsPlaying()) {
                desiredAudio.Play();
            }
            if (m_SyncLayers) {
                AudioHandle.Sync(m_CurrentSingleAudio, desiredAudio);
            }
            m_CurrentSingleAudio = desiredAudio;
        }

        private void UpdateMultiTrackMode() {
            ApplyGroupState(ref m_BaseAudio, m_BaseEventId, true, false);
            foreach(var layer in m_Layers) {
                ApplyGroupState(ref layer.Group, layer.Id.IsEmpty ? layer.EventId : layer.Id, layer.Enabled, true);
            }
        }

        private void ApplyGroupState(ref AudioGroup group, StringHash32 groupId, bool state, bool allowSync) {
            if (group.AppliedState == state) {
                return;
            }

            group.AppliedState = state;
            if (state) {
                Log.Msg("[ScriptAudioTrackVariants] Enabling track {0}", groupId);
                group.Handle.SetVolume(1, m_CrossFadeDuration, m_FadeInCurve);
                if (group.Handle.IsPaused()) {
                    group.Handle.Resume();
                } else if (!group.Handle.IsPlaying()) {
                    group.Handle.Play();
                }
                if (allowSync && m_SyncLayers) {
                    AudioHandle.Sync(m_BaseAudio.Handle, group.Handle);
                }
            } else {
                Log.Msg("[ScriptAudioTrackVariants] Disabling track {0}", groupId);
                group.Handle.SetVolume(0, m_CrossFadeDuration, m_FadeOutCurve);
            }
        }

        private void PauseHiddenTracks() {
            CheckPauseTrack(ref m_BaseAudio.Handle);
            foreach(var layer in m_Layers) {
                CheckPauseTrack(ref layer.Group.Handle);
            }
        }

        private void CheckPauseTrack(ref AudioHandle audio) {
            if (audio.IsPlaying() && audio.GetVolume() == 0) {
                audio.Pause();
            }
        }

        private Layer GetLayer(StringHash32 id) {
            foreach(var layer in m_Layers) {
                if (layer.Id == id) {
                    return layer;
                }
            }
            return null;
        }

        #endregion // Playback

        #region Handlers

        private void OnPlayerEnterLayer(Layer layer) {
            if (!isActiveAndEnabled)
                return;

            layer.Enabled = true;
            UpdatePlayingTracks();
        }

        private void OnPlayerExitLayer(Layer layer) {
            if (!isActiveAndEnabled || layer.Persistent)
                return;

            layer.Enabled = false;
            UpdatePlayingTracks();
        }

        #endregion // Handlers

        [LeafMember("SetAudioLayer"), Preserve]
        public void SetLayerActive(StringHash32 id, bool active) {
            Layer layer = GetLayer(id);
            if (layer != null) {
                if (layer.Enabled != active) {
                    layer.Enabled = active;
                    UpdatePlayingTracks();
                }
            }
        }
    }
}