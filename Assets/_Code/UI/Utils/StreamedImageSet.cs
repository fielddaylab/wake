using UnityEngine;

namespace Aqua {
    public struct StreamedImageSet {
        public readonly string Path;
        public readonly Sprite Fallback;

        public StreamedImageSet(string inPath) : this(inPath, null) { }

        public StreamedImageSet(Sprite inFallback) : this(null, inFallback) { }

        public StreamedImageSet(string inPath, Sprite inFallback) {
            Path = inPath;
            Fallback = inFallback;
        }

        public bool IsEmpty {
            get { return string.IsNullOrEmpty(Path) && Fallback == null; }
        }

        static public implicit operator StreamedImageSet(Sprite inSprite) {
            return new StreamedImageSet(inSprite);
        }
    }
}