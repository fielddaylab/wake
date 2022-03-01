using UnityEngine;

namespace EasyAssetStreaming {
    public interface IStreamingTextureComponent : IStreamingComponent {
        Texture2D Texture { get; }
        Rect UVRect { get; set; }
        
        Color Color { get; set; }
        float Alpha { get; set; }
        bool Visible { get; set; }
        
        AutoSizeMode SizeMode { get; set; }
        void Resize(AutoSizeMode sizeMode);
    }
}