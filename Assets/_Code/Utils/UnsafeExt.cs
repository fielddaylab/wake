using System;
using System.Collections.Generic;
using BeauUtil;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

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

        /// <summary>
        /// Converts an unmanaged buffer to a unity NativeArray.
        /// </summary>
        static public NativeArray<T> ToNativeArray<T>(T* ptr, int length, Unity.Collections.Allocator allocator) where T : unmanaged {
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, sizeof(T) * length, Allocator.None);
        }

        /// <summary>
        /// Hashes the given unmanaged struct.
        /// </summary>
        static public ulong Hash<T>(T value) where T : unmanaged {
            // fnv-1a
            ulong hash = 14695981039346656037;
            byte* ptr = (byte*) &value;
            int length = sizeof(T);
            while(length-- > 0) {
                hash = (hash ^ *ptr++) * 1099511628211;
            }
            return hash;
        }
    }
}