using BeauPools;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using UnityEngine;

namespace Aqua {
    static public class Formatting {
        private const string ScrambleChars = "()abc%defghijklmnopă©§^$&#¤ □0123456789[]qrstuvwxyz|_-";
        static private readonly int ScrambleCharLength = ScrambleChars.Length;

        static public unsafe string Scramble(StringSlice text, uint initialSeed = 0) {
            return Scramble(text, ref initialSeed);
        }

        static public unsafe string Scramble(StringSlice text, ref uint initialSeed) {
            char* buffer = Frame.AllocArray<char>(text.Length);
            char* write = buffer;
            uint seed = text.Hash32().HashValue ^ initialSeed;
            int i = 0;
            ushort c;
            while(i < text.Length) {
                c = text[i++];
                if ((c == ' ' || c == '\n') && PseudoRandom(ref seed, 64, c) < 50) {
                    *write++ = (char) c;
                } else {
                    if (i < text.Length - 1 && PseudoRandom(ref seed, 64, c) == 0) {
                        c ^= text[i++];
                    }
                    *write++ = ScrambleChars[PseudoRandom(ref seed, ScrambleCharLength, c)];
                }
            }

            return new string(buffer, 0, (int) (write - buffer));
        }

        static public unsafe string ScrambleLoc(TextId textId) {
            return Scramble(Loc.Find(textId), textId.Hash().HashValue);
        }

        static public int PseudoRandom(ref uint seed, int range, uint mod = 0) {
            seed = (uint) (((ulong) seed * 48271 * (mod + 1)) % 0x7FFFFFFF);
            return (int) (seed % range);
        }

        static public float PseudoRandom(ref uint seed, float min, float max, uint mod = 0) {
            float rand = PseudoRandom(ref seed, ushort.MaxValue, mod) / (float) ushort.MaxValue;
            return min + (max - min) * rand;
        }
    }
}