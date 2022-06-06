using System;

namespace EasyBugReporter {
    internal struct WorkQueue<T> {
        public readonly T[] Data;
        public readonly int Capacity;
        public int ReadHead;
        public int WriteHead;
        public int Count;

        public WorkQueue(int capacity) {
            Data = new T[capacity];
            Capacity = capacity;

            ReadHead = 0;
            WriteHead = 0;
            Count = 0;
        }

        public void Enqueue(T item) {
            if (Count == Capacity) {
                throw new InvalidOperationException(string.Format("WorkQueue<{0}> is already full (capacity {1})!", typeof(T).Name, Capacity));
            }

            Count++;
            Data[WriteHead] = item;
            WriteHead = (WriteHead + 1) % Capacity;
        }

        public ref T Peek() {
            return ref Data[ReadHead];
        }

        public T Dequeue() {
            if (Count > 0) {
                T val = Data[ReadHead];
                ReadHead = (ReadHead + 1) % Capacity;
                Count--;
                return val;
            }
            return default;
        }

        public T this[int idx] {
            get { return Data[(ReadHead + idx) % Capacity]; }
        }

        public void Reset() {
            Array.Clear(Data, 0, Capacity);
            ReadHead = WriteHead = 0;
            Count = 0;
        }
    }
}