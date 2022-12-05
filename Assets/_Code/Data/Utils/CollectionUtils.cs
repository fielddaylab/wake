using System.Collections.Generic;
using System.Reflection;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public static class CollectionUtils
    {
        static public void ObjectArray<T>(this Serializer ioSerializer, string inKey, ref RingBuffer<T> ioBuffer) where T : ISerializedObject
        {
            T[] array = null;
            if (ioSerializer.IsWriting && ioBuffer != null)
                array = ioBuffer.ToArray();

            ioSerializer.ObjectArray(inKey, ref array);

            if (ioSerializer.IsReading)
            {
                ioBuffer.Clear();
                ioBuffer.SetCapacity(Mathf.NextPowerOfTwo(array.Length));
                for(int i = 0; i < array.Length; i++)
                    ioBuffer.PushBack(array[i]);
            }
        }

        static private readonly object[] s_HashSetInitializeInvokeArr = new object[1];

        static public void Initialize<T>(this HashSet<T> set, int capacity)
        {
            s_HashSetInitializeInvokeArr[0] = capacity;
            set.GetType().GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Invoke(set, s_HashSetInitializeInvokeArr);
        }

        static public HashSet<T> CreateSet<T>(int capacity)
        {
            HashSet<T> set = new HashSet<T>(CompareUtils.DefaultComparer<T>());
            set.Initialize(capacity);
            return set;
        }
    }
}