using UnityEngine;

namespace Aqua {
    public struct StreamedImageSet {
        public readonly string Path;
        public readonly Sprite Fallback;
        public readonly string Tooltip;

        public StreamedImageSet(string inPath, string inTooltip = null) : this(inPath, null, inTooltip) { }

        public StreamedImageSet(Sprite inFallback, string inTooltip = null) : this(null, inFallback, inTooltip) { }

        public StreamedImageSet(string inPath, Sprite inFallback, string inTooltip = null) {
            Path = inPath;
            Fallback = inFallback;
            Tooltip = inTooltip;
        }

        public bool IsEmpty {
            get { return string.IsNullOrEmpty(Path) && Fallback == null; }
        }

        static public implicit operator StreamedImageSet(Sprite inSprite) {
            return new StreamedImageSet(inSprite);
        }
    }
}