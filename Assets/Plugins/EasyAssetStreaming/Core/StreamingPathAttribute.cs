using UnityEngine;

namespace EasyAssetStreaming {

    /// <summary>
    /// Streaming path selector.
    /// </summary>
    public class StreamingPathAttribute : PropertyAttribute {
        public string Filter { get; set; }

        public StreamingPathAttribute() { }
        public StreamingPathAttribute(string filter) {
            Filter = filter;
        }
    }

    /// <summary>
    /// Streaming path for png, jpg, and jpeg files.
    /// </summary>
    public class StreamingImagePathAttribute : StreamingPathAttribute {
        public StreamingImagePathAttribute()
            : base("png,jpg,jpeg")
        { }
    }

    /// <summary>
    /// Streaming path for mp4 and webm files.
    /// </summary>
    public class StreamingVideoPathAttribute : StreamingPathAttribute {
        public StreamingVideoPathAttribute()
            : base("webm,mp4")
        { }
    }

    /// <summary>
    /// Streaming path for mp3, ogg, wav, acc, and webm files.
    /// </summary>
    public class StreamingAudioPathAttribute : StreamingPathAttribute {
        public StreamingAudioPathAttribute()
            : base("mp3,ogg,wav,aac")
        { }
    }
}