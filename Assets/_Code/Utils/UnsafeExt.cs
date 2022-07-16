using System;
using System.Collections.Generic;
using BeauUtil;

namespace Aqua {
    static public unsafe class UnsafeExt {

        static public string PrePostString(string inText, string inPrefix, string inPostfix) {
            inPrefix = inPrefix ?? string.Empty;
            inPostfix = inPostfix ?? string.Empty;

            if (inPrefix.Length + inPostfix.Length <= 0) {
                return inText;
            }

            int charBufferSize = inText.Length + inPrefix.Length + inPostfix.Length;
            char* charBuffer = stackalloc char[charBufferSize];

            char* charHead = charBuffer;
            int remaining = charBufferSize;
            
            int len = inPrefix.Length;
            if (len > 0) {
                fixed(char* pre = inPrefix) {
                    Unsafe.CopyArray(pre, len, charHead);
                    charHead += len;
                    remaining -= len;
                }
            }
            
            fixed(char* txt = inText) {
                len = inText.Length;
                Unsafe.CopyArray(txt, len, charHead);
                charHead += len;
                remaining -= len;
            }
            
            len = inPostfix.Length;
            if (len > 0) {
                fixed(char* post = inPostfix) {
                    Unsafe.CopyArray(post, len, charHead);
                }
            }

            return new string(charBuffer, 0, charBufferSize);
        }
    }
}