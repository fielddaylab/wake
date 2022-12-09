using System;
using System.Collections.Generic;
using System.Reflection;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public static class Collections
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
            MethodInfo initMethod = MethodsCache<T>.Retrieve(ref MethodsCache<T>.HashSetInitialize, typeof(HashSet<T>), "Initialize");
            s_HashSetInitializeInvokeArr[0] = capacity; 
            initMethod.Invoke(set, s_HashSetInitializeInvokeArr);
        }

        static public HashSet<T> NewSet<T>(int capacity)
        {
            HashSet<T> set = new HashSet<T>(CompareUtils.DefaultComparer<T>());
            set.Initialize(capacity);
            return set;
        }

        static public Dictionary<K, V> NewDictionary<K, V>(int capacity)
        {
            return new Dictionary<K, V>(capacity, CompareUtils.DefaultComparer<K>());
        }

        static private class MethodsCache<T>
        {
            static public MethodInfo HashSetInitialize;

            static public MethodInfo Retrieve(ref MethodInfo info, Type type, string methodName) {
                if (info == null) {
                    info = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                return info;
            }
        }
    }
}