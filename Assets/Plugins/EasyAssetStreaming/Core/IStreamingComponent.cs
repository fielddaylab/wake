using UnityEngine;
using UnityEngine.Events;

namespace EasyAssetStreaming {
    public interface IStreamingComponent {
        string Path { get; set; }
        bool IsLoading();
        bool IsLoaded();
        void Preload();
        void Unload();

        event StreamingComponentEvent OnUpdated;
    }

    public delegate void StreamingComponentEvent(IStreamingComponent component, Streaming.AssetStatus status);
}