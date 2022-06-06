using System;

namespace EasyBugReporter {
    internal struct WindowBuffer<T> {
        public readonly T[] Data;
        public readonly int Capacity;
        public int Head;
        public int Count;

        public ulong Total;

        public WindowBuffer(int capacity) {
            Data = new T[capacity];
            Capacity = capacity;

            Head = 0;
            Count = 0;
            Total = 0;
        }

        public void Write(T item) {
            Data[Head] = item;
            Head = (Head + 1) % Capacity;
            if (Count < Capacity) {
                Count++;
            }
            Total++;
        }

        public T this[int idx] {
            get { return Data[(Head - Count + Capacity + idx) % Capacity]; }
        }

        public void Reset() {
            Array.Clear(Data, 0, Capacity);
            Head = 0;
            Count = 0;
            Total = 0;
        }
    }
}