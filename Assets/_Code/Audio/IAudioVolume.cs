using UnityEngine;

namespace AquaAudio
{
    public interface IAudioVolume {
        void UpdateFromListener(Vector3 listenerPos, Vector3 avatarPos);
        void UpdateCache();
    }
}