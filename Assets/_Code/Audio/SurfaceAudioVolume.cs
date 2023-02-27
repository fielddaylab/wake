using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace AquaAudio
{
    public class SurfaceAudioVolume : MonoBehaviour, IAudioVolume {
        public SerializedHash32 StreamEventId;
        public float MinDepth = 5;
        public float MaxDepth = 12;
        public Curve VolumeCurve = Curve.CubeIn;

        private AudioHandle m_Handle;
        private float m_SourceY;

        private void Start() {
            m_Handle = Services.Audio.PostEvent(StreamEventId, AudioPlaybackFlags.PreloadOnly);
            Script.OnSceneLoad(OnSceneLoad);
        }

        private void OnSceneLoad() {
            m_Handle.Play().SetVolume(0);
        }

        private void OnEnable() {
            Services.Audio.RegisterVolume(this);
        }

        private void OnDisable() {
            Services.Audio?.DeregisterVolume(this);
            m_Handle.Stop(0.1f);
        }

        public void UpdateCache() {
            m_SourceY = transform.position.y;
        }

        public void UpdateFromListener(Vector3 listenerPos, Vector3 avatarPos) {
            if (m_Handle.IsReady()) {
                float dist = Math.Abs(listenerPos.y - m_SourceY);
                dist = Math.Min(Math.Abs(avatarPos.y - m_SourceY), dist);
                dist = Math.Max(dist - MinDepth, 0) / (MaxDepth - MinDepth);
                if (dist > 1) {
                    m_Handle.Pause();
                } else {
                    m_Handle.Resume();
                    m_Handle.SetVolume(TweenUtil.Evaluate(VolumeCurve, 1 - dist));
                }
            }
        }
    }
}