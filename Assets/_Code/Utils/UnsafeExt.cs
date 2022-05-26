using System;
using System.Collections.Generic;
using BeauUtil;

namespace Aqua {
    static public unsafe class UnsafeExt {

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
                Quicksort(buffer, lower, pivot);
                Quicksort(buffer, pivot + 1, higher);
            }
        }

        static public void Quicksort<T>(T* buffer, int lower, int higher, IComparer<T> comparison) where T : unmanaged {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher, comparison);
                Quicksort(buffer, lower, pivot);
                Quicksort(buffer, pivot + 1, higher);
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
    
        #endregion // Quicksort

        public delegate int ComparisonPtr<T>(T* x, T* y) where T : unmanaged;
    }
}