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
                    Unsafe.Copy(pre, len, charHead, remaining);
                    charHead += len;
                    remaining -= len;
                }
            }
            
            fixed(char* txt = inText) {
                len = inText.Length;
                Unsafe.Copy(txt, len, charHead, remaining);
                charHead += len;
                remaining -= len;
            }
            
            len = inPostfix.Length;
            if (len > 0) {
                fixed(char* post = inPostfix) {
                    Unsafe.Copy(post, len, charHead, remaining);
                }
            }

            return new string(charBuffer, 0, charBufferSize);
        }

        #region Quicksort

        static public void Quicksort<T>(T* buffer, int count) where T : unmanaged {
            Quicksort<T>(buffer, 0, count - 1);
        }

        static public void Quicksort<T>(T* buffer, int count, ComparisonPtr<T> comparison) where T : unmanaged {
            Quicksort<T>(buffer, 0, count - 1, comparison);
        }

        static public void Quicksort<T>(T* buffer, int count, IComparer<T> comparison) where T : unmanaged {
            Quicksort<T>(buffer, 0, count - 1, comparison);
        }

        static public void Quicksort<T>(T* buffer, int count, ComparisonIndex<T> indexGen) where T : unmanaged {
            Quicksort<T>(buffer, 0, count - 1, indexGen);
        }

        static public void Quicksort<T>(T* buffer, int lower, int higher) where T : unmanaged {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher, Comparer<T>.Default);
                Quicksort(buffer, lower, pivot);
                Quicksort(buffer, pivot + 1, higher);
            }
        }

        static public void Quicksort<T>(T* buffer, int lower, int higher, ComparisonPtr<T> comparison) where T : unmanaged {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher, comparison);
                Quicksort(buffer, lower, pivot, comparison);
                Quicksort(buffer, pivot + 1, higher, comparison);
            }
        }

        static public void Quicksort<T>(T* buffer, int lower, int higher, IComparer<T> comparison) where T : unmanaged {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher, comparison);
                Quicksort(buffer, lower, pivot, comparison);
                Quicksort(buffer, pivot + 1, higher, comparison);
            }
        }

        static public void Quicksort<T>(T* buffer, int lower, int higher, ComparisonIndex<T> indexGen) where T : unmanaged {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher, indexGen);
                Quicksort(buffer, lower, pivot, indexGen);
                Quicksort(buffer, pivot + 1, higher, indexGen);
            }
        }

        static private int Partition<T>(T* buffer, int lower, int higher, ComparisonPtr<T> comparison) where T : unmanaged {
            int center = (lower + higher) >> 1;

            int i = lower - 1;
            int j = higher + 1;

            while(true) {
                do {
                    i++;
                } while (comparison(&buffer[i], &buffer[center]) < 0);
                do {
                    j--;
                } while (comparison(&buffer[j], &buffer[center]) > 0);

                if (i >= j) {
                    return j;
                }

                Ref.Swap(ref buffer[i], ref buffer[j]);
            }
        }

        static private int Partition<T>(T* buffer, int lower, int higher, IComparer<T> comparison) where T : unmanaged {
            int center = (lower + higher) >> 1;
            
            int i = lower - 1;
            int j = higher + 1;

            while(true) {
                do {
                    i++;
                } while (comparison.Compare(buffer[i], buffer[center]) < 0);
                do {
                    j--;
                } while (comparison.Compare(buffer[j], buffer[center]) > 0);

                if (i >= j) {
                    return j;
                }

                Ref.Swap(ref buffer[i], ref buffer[j]);
            }
        }

        static private int Partition<T>(T* buffer, int lower, int higher, ComparisonIndex<T> indexGen) where T : unmanaged {
            int center = (lower + higher) >> 1;
            int pivotVal = indexGen(&buffer[center]);
            
            int i = lower - 1;
            int j = higher + 1;

            while(true) {
                do {
                    i++;
                } while (indexGen(&buffer[i]) < pivotVal);
                do {
                    j--;
                } while (indexGen(&buffer[j]) > pivotVal);

                if (i >= j) {
                    return j;
                }

                Ref.Swap(ref buffer[i], ref buffer[j]);
            }
        }
    
        #endregion // Quicksort

        public delegate int ComparisonPtr<T>(T* x, T* y) where T : unmanaged;
        public delegate int ComparisonIndex<T>(T* x) where T : unmanaged;
    }
}