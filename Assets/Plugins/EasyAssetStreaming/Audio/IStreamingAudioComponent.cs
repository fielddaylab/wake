using UnityEngine;

namespace EasyAssetStreaming {
    public interface IStreamingAudioComponent : IStreamingComponent {
        AudioClip Clip { get; }

        bool IsPlaying { get; }
        
        float Volume { get; set; }
        float Pitch { get; set; }

        bool Loop { get; set; }
        bool Mute { get; set; }

        float Duration { get; }
        float Time { get; set; }

        void Pause();
        void Play();
        void Play(float time);
        void Stop();
        void UnPause();
    }
}