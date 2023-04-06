using UnityEngine;
using Aqua;
using AquaAudio;
using Aqua.Scripting;
using BeauUtil;
using System.Collections;
using BeauRoutine;
using Leaf.Runtime;

namespace ProtoAqua.Observation {
    public class PatternAudio : ScriptComponent {
        [Header("Setup")]
        [SerializeField] private SerializedHash32 m_DotSFX = "Secret.Dot3D";
        [SerializeField] private SerializedHash32 m_DashSFX = "Secret.Dash3D";
        [SerializeField] private float m_UnitTiming = 0.1f;

        [Header("Pattern")]
        public string Pattern = "... --- ...";
        public SerializedHash32 PatternId = null;
        public bool PlayOnAwake = true;
        [Range(0.01f, 4)] public float PlaybackRate = 1;
        [Range(0, 1)] public float PlaybackVolume = 1;
        [Range(0.1f, 3)] public float PlaybackPitch = 1;
        public int PatternEndDelay = 16;
        public Transform Location = null;

        private Routine m_Playback;
        private AudioHandle m_CurrentDot;
        private AudioHandle m_CurrentDash;

        private void Start() {
            if (PlayOnAwake) {
                if (Script.IsLoading) {
                    Script.OnSceneLoad(Play);
                } else {
                    Play();
                }
            }
        }

        [LeafMember("StartMorse")]
        public void Play() {
            if (!m_Playback) {
                m_Playback = Routine.Start(this, Playback());
            }
        }

        [LeafMember("ReplaceMorse")]
        public void ReplacePattern(string newPattern, bool play = false) {
            Pattern = newPattern;
            if (play) {
                m_Playback.Replace(this, Playback());
            } else {
                m_Playback.Stop();
            }
        }

        [LeafMember("StopMorse")]
        public void Stop() {
            m_Playback.Stop();
        }

        private IEnumerator Playback() {
            int idx = -1;
            char c;
            char next;
            float delay = 0;
            int patternLength;

            if (!PatternId.IsEmpty) {
                var patternDataTweak = Services.Tweaks.Get<PatternData>();
                if (patternDataTweak != null && patternDataTweak.TryGetEntry(PatternId, out string newPattern, out float pitch)) {
                    Pattern = newPattern;
                    if (pitch != 0) {
                        PlaybackPitch = pitch;
                    }
                }
            }

            while(!string.IsNullOrEmpty(Pattern)) {
                patternLength = Pattern.Length;
                idx = (idx + 1) % patternLength;
                c = Pattern[idx];
                if (idx < patternLength - 1) {
                    next = Pattern[idx + 1];
                } else {
                    next = (char) 0;
                }

                delay = 0;

                switch(c) {
                    case '.': { // dot
                        m_CurrentDot.Stop();
                        m_CurrentDot = Services.Audio.PostEvent(m_DotSFX).TrackPosition(Location).SetVolume(PlaybackVolume).SetPitch(PlaybackPitch);
                        delay = 1;
                        break;
                    }
                    case '-': { // dash
                        m_CurrentDash.Stop();
                        m_CurrentDash = Services.Audio.PostEvent(m_DashSFX).TrackPosition(Location).SetVolume(PlaybackVolume).SetPitch(PlaybackPitch);
                        delay = 3;
                        break;
                    }
                    case ' ': { // letter space
                        if (next != '/') { // ...but only if we're not using a " / "
                            delay = 3;
                        }
                        break;
                    }
                    case '/': { // word space
                        delay = 7;
                        if (next == ' ') { // skip next space
                            idx++;
                        }
                        break;
                    }
                    default: { // wtf
                        delay = 0;
                        break;
                    }
                }

                if (delay > 0) {
                    if (idx == Pattern.Length - 1) { // slightly longer delay at end of message
                        delay += PatternEndDelay;
                    }
                    delay = (delay + 1) * (m_UnitTiming * (PlaybackRate * PlaybackPitch));
                    while(delay > 0) {
                        yield return null;
                        delay -= Routine.DeltaTime;
                    }
                }
            }
        }
    }
}