namespace Aqua {
    static public class PseudoRandom {
        static public int Int(ref uint seed, int range, uint mod = 0) {
            seed = (uint) (((ulong) seed * 48271 * (mod + 1)) % 0x7FFFFFFF);
            return (int) (seed % range);
        }

        static public float Float(ref uint seed, float min, float max, uint mod = 0) {
            float rand = Int(ref seed, ushort.MaxValue, mod) / (float) ushort.MaxValue;
            return min + (max - min) * rand;
        }
    }
}