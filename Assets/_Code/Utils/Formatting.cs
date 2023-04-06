using Aqua.Scripting;
using BeauPools;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using UnityEngine;

namespace Aqua {
    static public class Formatting {
        private const string ScrambleChars = "()abc%def+ghijklmnop^$&#ABCDEF0123456789[]qrstuvwxyz|_-";
        static private readonly int ScrambleCharLength = ScrambleChars.Length;

        private const int MaxUnscrambledSegments = 8;

        static public unsafe string Scramble(StringSlice text, uint initialSeed = 0) {
            return Scramble(text, ref initialSeed);
        }

        static public unsafe string Scramble(StringSlice text, ref uint initialSeed) {
            char* buffer = Frame.AllocArray<char>(text.Length);
            char* write = buffer;
            uint seed = StringHash32.Fast(text).HashValue ^ initialSeed;

            Scramble(text, ref seed, ref write);

            initialSeed = seed;
            return new string(buffer, 0, (int) (write - buffer));
        }

        static public unsafe string ScrambleTagged(TagString tag, ref uint initialSeed, string unscrambledColorTag) {
            string richText = tag.RichText;

            fixed(char* pinnedText = richText) {
                char* buffer = Frame.AllocArray<char>(richText.Length + MaxUnscrambledSegments * (8 + unscrambledColorTag.Length));
                char* write = buffer;
                uint seed = StringHash32.Fast(richText).HashValue ^ initialSeed;

                var nodes = tag.Nodes;
                TagNodeData node;
                bool scrambleMode = true;

                for(int index = 0; index < nodes.Length; index++) {
                    node = nodes[index];
                    switch(node.Type) {
                        case TagNodeType.Event: {
                            if (node.Event.Type == ScriptEvents.Global.Unscramble) {
                                scrambleMode = node.Event.IsClosing;
                            }
                            break;
                        }

                        case TagNodeType.Text: {
                            int offset = node.Text.RichCharacterOffset, count = node.Text.RichCharacterCount;
                            if (scrambleMode) {
                                Scramble(pinnedText + offset, count, ref seed, ref write);
                            } else {
                                Write(unscrambledColorTag, ref write);
                                Unsafe.CopyArray(pinnedText + offset, count, write, (int) (write - buffer));
                                write += count;
                                Write("</color>", ref write);
                            }
                            break;
                        }
                    }
                }

                initialSeed = seed;
                return new string(buffer, 0, (int) (write - buffer));
            }
        }

        static private unsafe int Scramble(StringSlice segment, ref uint seed, ref char* writeHead) {
            int i = 0;
            ushort c;
            int length = 0;
            while(i < segment.Length) {
                c = segment[i++];
                if ((c == ' ' || c == '\n') && PseudoRandom.Int(ref seed, 64, c) < 50) {
                    *writeHead++ = (char) c;
                } else {
                    if (i < segment.Length - 1 && PseudoRandom.Int(ref seed, 64, c) == 0) {
                        c ^= segment[i++];
                    }
                    *writeHead++ = ScrambleChars[PseudoRandom.Int(ref seed, ScrambleCharLength, c)];
                }
                length++;
            }
            return length;
        }

        static private unsafe int Scramble(char* segment, int segmentLength, ref uint seed, ref char* writeHead) {
            int i = 0;
            ushort c;
            int length = 0;
            while(i < segmentLength) {
                c = segment[i++];
                if ((c == ' ' || c == '\n') && PseudoRandom.Int(ref seed, 64, c) < 50) {
                    *writeHead++ = (char) c;
                } else {
                    if (i < segmentLength - 1 && PseudoRandom.Int(ref seed, 64, c) == 0) {
                        c ^= segment[i++];
                    }
                    *writeHead++ = ScrambleChars[PseudoRandom.Int(ref seed, ScrambleCharLength, c)];
                }
                length++;
            }
            return length;
        }

        static private unsafe void Write(string constant, ref char* writeHead) {
            fixed(char* pin = constant) {
                Unsafe.CopyArray(pin, constant.Length, writeHead, constant.Length);
                writeHead += constant.Length;
            }
        }

        static public unsafe string ScrambleLoc(TextId textId) {
            return Scramble(Loc.Find(textId), textId.Hash().HashValue);
        }

        static public unsafe string ScrambleLocTagged(TextId textId, string colorTag) {
            TagString tag = Parsing.WorkingTag;
            Services.Loc.LocalizeTagged(ref tag, textId);
            uint hash = textId.Hash().HashValue;
            string result = ScrambleTagged(tag, ref hash, colorTag);
            tag.Clear();
            return result;
        }
    }
}