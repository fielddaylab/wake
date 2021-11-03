namespace Aqua {
    static public class Frame {
        public const ushort InvalidIndex = ushort.MaxValue;

        static public ushort Index;

        static internal void IncrementFrame() {
            Index = (ushort) ((Index + 1) % InvalidIndex);
        }
    }
}