using UnityEngine;

namespace EasyAssetStreaming {
    public interface IStreamingTextureComponent : IStreamingComponent {
        Texture Texture { get; }
        Rect UVRect { get; set; }
        
        Color Color { get; set; }
        float Alpha { get; set; }
        bool Visible { get; set; }
        
        Vector2 Size { get; set; }
        AutoSizeMode SizeMode { get; set; }
        void Resize(AutoSizeMode sizeMode);
    }
}