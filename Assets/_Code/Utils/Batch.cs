using System;
using System.Collections.Generic;
using System.Collections;

namespace BeauUtil {
    /// <summary>
    /// Batch processing helpers.
    /// </summary>
    static public class Batch {

        #region Sorting

        /// <summary>
        /// Sorts a list by batch id.
        /// </summary>
        static public void Sort<T>(List<T> objects) where T : IBatchId {
            objects.Sort(BatchIdSorter<T>.Instance);
        }

        /// <summary>
        /// Sorts a list by batch id.
        /// </summary>
        static public void Sort<T>(RingBuffer<T> objects) where T : IBatchId {
            objects.Sort(BatchIdSorter<T>.Instance);
        }

        /// <summary>
        /// Sorts an array by batch id.
        /// </summary>
        static public void Sort<T>(T[] objects) where T : IBatchId {
            Array.Sort(objects, BatchIdSorter<T>.Instance);
        }

        /// <summary>
        /// Sorts an array range by batch id.
        /// </summary>
        static public void Sort<T>(T[] objects, int startIdx, int length) where T : IBatchId {
            Array.Sort(objects, startIdx, length, BatchIdSorter<T>.Instance);
        }

        /// <summary>
        /// Sorts a buffer by batch id.
        /// </summary>
        static public unsafe void Sort<T>(T* objects, int length) where T : unmanaged, IBatchId {
            Unsafe.Quicksort(objects, length, BatchIdSorter<T>.Instance);
        }

        /// <summary>
        /// Returns the IComparer to sort the given type by its batch id.
        /// </summary>
        static public IComparer<T> Comparer<T>() where T : IBatchId {
            return BatchIdSorter<T>.Instance;
        }

        /// <summary>
        /// Returns the Comparison to sort the given type by its batch id.
        /// </summary>
        static public Comparison<T> Comparison<T>() where T : IBatchId {
            return BatchIdSorter<T>.AsFunc;
        }

        private class BatchIdSorter<T> : IComparer<T> where T : IBatchId {
            static public readonly BatchIdSorter<T> Instance = new BatchIdSorter<T>();
            static public readonly Comparison<T> AsFunc = (x, y) => {
                return x.BatchId - y.BatchId;
            };

            public int Compare(T x, T y) {
                return x.BatchId - y.BatchId;
            }
        }

        #endregion // Sorting

        #region Process

        /// <summary>
        /// Delegate for processing a batch of items.
        /// </summary>
        public delegate void ProcessDelegate<TItem>(ListSlice<TItem> items, int batchId) where TItem : IBatchId;

        /// <summary>
        /// Delegate for processing a batch of items.
        /// </summary>
        public delegate void ProcessDelegate<TItem, TArgs>(ListSlice<TItem> items, int batchId, in TArgs inArgs) where TItem : IBatchId where TArgs : struct;

        /// <summary>
        /// Delegate for processing a batch of items.
        /// </summary>
        public unsafe delegate void UnsafeProcessDelegate<TItem>(TItem* items, int itemCount, int batchId) where TItem : unmanaged, IBatchId;

        /// <summary>
        /// Delegate for processing a batch of items.
        /// </summary>
        public unsafe delegate void UnsafeProcessDelegate<TItem, TArgs>(TItem* items, int itemCount, int batchId, in TArgs inArgs) where TItem : unmanaged, IBatchId where TArgs : struct;

        /// <summary>
        /// Batch processor.
        /// </summary>
        public struct Processor<TItem> : IDisposable, IEnumerator
            where TItem : IBatchId
        {
            public int BatchSize;
            public ProcessDelegate<TItem> Process;

            public ListSlice<TItem> Items;
            public int CurrentIndex;

            object IEnumerator.Current { get { return null; } }

            /// <summary>
            /// Prepares to process the set of items.
            /// </summary>
            public void Prep(ListSlice<TItem> items) {
                Items = items;
                CurrentIndex = 0;
            }

            /// <summary>
            /// Clears the processor.
            /// </summary>
            public void Clear() {
                Items = default(ListSlice<TItem>);
                CurrentIndex = -1;
            }

            /// <summary>
            /// Processes all batches.
            /// </summary>
            public void ProcessAll() {
                while(MoveNext());
            }

            public void Dispose() {
                Clear();
            }

            /// <summary>
            /// Processes one batch.
            /// </summary>
            public bool MoveNext() {
                if (CurrentIndex >= Items.Length) {
                    return false;
                }

                int start = CurrentIndex;
                int batchId = Items[start].BatchId;
                int idx = start + 1;
                int maxEnd = BatchSize <= 0 ? Items.Length : Math.Min(start + BatchSize, Items.Length);

                for(; idx < maxEnd && Items[idx].BatchId == batchId; idx++);

                Process(new ListSlice<TItem>(Items, start, idx - start), batchId);
                return true;
            }

            void IEnumerator.Reset() {
                Clear();
            }
        }

        /// <summary>
        /// Batch processor.
        /// </summary>
        public struct Processor<TItem, TArgs> : IDisposable, IEnumerator
            where TItem : IBatchId
            where TArgs : struct
        {
            public int BatchSize;
            public ProcessDelegate<TItem, TArgs> Process;

            public ListSlice<TItem> Items;
            public int CurrentIndex;
            public TArgs Arguments;

            object IEnumerator.Current { get { return null; } }

            /// <summary>
            /// Prepares to process the set of items.
            /// </summary>
            public void Prep(ListSlice<TItem> items, TArgs args) {
                Items = items;
                CurrentIndex = 0;
                Arguments = args;
            }

            /// <summary>
            /// Clears the processor.
            /// </summary>
            public void Clear() {
                Items = default(ListSlice<TItem>);
                CurrentIndex = -1;
                Arguments = default(TArgs);
            }

            /// <summary>
            /// Processes all batches.
            /// </summary>
            public void ProcessAll() {
                while(MoveNext());
            }

            public void Dispose() {
                Clear();
            }

            /// <summary>
            /// Processes one batch.
            /// </summary>
            public bool MoveNext() {
                if (CurrentIndex >= Items.Length) {
                    return false;
                }

                int start = CurrentIndex;
                int batchId = Items[start].BatchId;
                int idx = start + 1;
                int maxEnd = BatchSize <= 0 ? Items.Length : Math.Min(start + BatchSize, Items.Length);

                for(; idx < maxEnd && Items[idx].BatchId == batchId; idx++);

                Process(new ListSlice<TItem>(Items, start, idx - start), batchId, Arguments);
                return true;
            }

            void IEnumerator.Reset() {
                Clear();
            }
        }

        /// <summary>
        /// Batch processor.
        /// </summary>
        public unsafe struct UnsafeProcessor<TItem> : IDisposable, IEnumerator
            where TItem : unmanaged, IBatchId
        {
            public int BatchSize;
            public UnsafeProcessDelegate<TItem> Process;

            public TItem* Items;
            public int Count;
            public int CurrentIndex;

            object IEnumerator.Current { get { return null; } }

            /// <summary>
            /// Prepares to process the set of items.
            /// </summary>
            public void Prep(TItem* items, int count) {
                Items = items;
                Count = count;
                CurrentIndex = 0;
            }

            /// <summary>
            /// Clears the processor.
            /// </summary>
            public void Clear() {
                Items = null;
                Count = 0;
                CurrentIndex = -1;
            }

            /// <summary>
            /// Processes all batches.
            /// </summary>
            public void ProcessAll() {
                while(MoveNext());
            }

            public void Dispose() {
                Clear();
            }

            /// <summary>
            /// Processes one batch.
            /// </summary>
            public bool MoveNext() {
                if (CurrentIndex >= Count) {
                    return false;
                }

                int start = CurrentIndex;
                int batchId = Items[start].BatchId;
                int idx = start + 1;
                int maxEnd = BatchSize <= 0 ? Count : Math.Min(start + BatchSize, Count);

                for(; idx < maxEnd && Items[idx].BatchId == batchId; idx++);

                Process(&Items[start], idx - start, batchId);
                return true;
            }

            void IEnumerator.Reset() {
                Clear();
            }
        }

        /// <summary>
        /// Batch processor.
        /// </summary>
        public unsafe struct UnsafeProcessor<TItem, TArgs> : IDisposable, IEnumerator
            where TItem : unmanaged, IBatchId
            where TArgs : struct
        {
            public int BatchSize;
            public UnsafeProcessDelegate<TItem, TArgs> Process;

            public TItem* Items;
            public int Count;
            public int CurrentIndex;
            public TArgs Arguments;

            object IEnumerator.Current { get { return null; } }

            /// <summary>
            /// Prepares to process the set of items.
            /// </summary>
            public void Prep(TItem* items, int count, TArgs args) {
                Items = items;
                Count = count;
                CurrentIndex = 0;
                Arguments = args;
            }

            /// <summary>
            /// Clears the processor.
            /// </summary>
            public void Clear() {
                Items = null;
                Count = 0;
                CurrentIndex = -1;
                Arguments = default(TArgs);
            }

            /// <summary>
            /// Processes all batches.
            /// </summary>
            public void ProcessAll() {
                while(MoveNext());
            }

            public void Dispose() {
                Clear();
            }

            /// <summary>
            /// Processes one batch.
            /// </summary>
            public bool MoveNext() {
                if (CurrentIndex >= Count) {
                    return false;
                }

                int start = CurrentIndex;
                int batchId = Items[start].BatchId;
                int idx = start + 1;
                int maxEnd = BatchSize <= 0 ? Count : Math.Min(start + BatchSize, Count);

                for(; idx < maxEnd && Items[idx].BatchId == batchId; idx++);

                Process(&Items[start], idx - start, batchId, Arguments);
                return true;
            }

            void IEnumerator.Reset() {
                Clear();
            }
        }

        #endregion // Process
    }

    /// <summary>
    /// Interface for an object specifying a batch id.
    /// </summary>
    public interface IBatchId {
        /// <summary>
        /// Batch identifier.
        /// </summary>
        int BatchId { get; }
    }
}