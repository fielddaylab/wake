/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    21 June 2022
 * 
 * File:    RuntimeObjectHandle.cs
 * Purpose: Weak runtime UnityEngine.Object handle. Allows for weak Object references in unmanaged structs.
            Also provides Object lookups based on unity's internal InstanceID.
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
#define OBJECT_HANDLE_PRESERVE_TYPE
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT

using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    /// <summary>
    /// Weak runtime UnityEngine.Object handle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeObjectHandle : IEquatable<RuntimeObjectHandle>, IEquatable<UnityEngine.Object>, IComparable<RuntimeObjectHandle> {
        
        /// <summary>
        /// Null handle.
        /// </summary>
        static public readonly RuntimeObjectHandle Null = new RuntimeObjectHandle(0);

        /// <summary>
        /// Instance Id.
        /// </summary>
        public readonly int Id;

        #if OBJECT_HANDLE_PRESERVE_TYPE

        private readonly RuntimeTypeHandle m_TypeHandle;

        #endif // OBJECT_HANDLE_PRESERVE_TYPE

        public RuntimeObjectHandle(int instanceId) {
            Id = instanceId;
            #if OBJECT_HANDLE_PRESERVE_TYPE
            m_TypeHandle = default;
            #endif // OBJECT_HANDLE_PRESERVE_TYPE
        }

        public RuntimeObjectHandle(UnityEngine.Object obj) {
            if (ReferenceEquals(obj, null)) {
                Id = 0;
                #if OBJECT_HANDLE_PRESERVE_TYPE
                m_TypeHandle = default;
                #endif // OBJECT_HANDLE_PRESERVE_TYPE
            } else {
                Id = obj.GetInstanceID();
                #if OBJECT_HANDLE_PRESERVE_TYPE
                m_TypeHandle = obj.GetType().TypeHandle;
                #endif // OBJECT_HANDLE_PRESERVE_TYPE
            }
        }

        #region Cast

        /// <summary>
        /// Returns the Object with this handle.
        /// </summary>
        public UnityEngine.Object Object {
            [MethodImpl(256)] get { return Find(Id); }
        }

        /// <summary>
        /// Returns the Object with this handle.
        /// </summary>
        [MethodImpl(256)]
        public T Cast<T>() where T : UnityEngine.Object {
            return Find<T>(Id);
        }

        /// <summary>
        /// Returns the Object with this handle.
        /// </summary>
        [MethodImpl(256)]
        public T SafeCast<T>() where T : UnityEngine.Object {
            return SafeFind<T>(Id);
        }

        /// <summary>
        /// Returns if the Object with this handle is alive.
        /// </summary>
        [MethodImpl(256)]
        public bool IsAlive() {
            return IsAlive(Id);
        }

        /// <summary>
        /// Returns if this Object was previously valid but is now destroyed.
        /// </summary>
        [MethodImpl(256)]
        public bool WasDestroyed() {
            return Id != 0 && !IsAlive(Id);
        }

        #endregion // Cast

        #region Interfaces

        [MethodImpl(256)]
        public bool Equals(RuntimeObjectHandle other) {
            return Id == other.Id;
        }

        [MethodImpl(256)]
        public bool Equals<T>(RuntimeObjectHandle<T> other)
            where T : UnityEngine.Object {
            return Id == other.Id;
        }

        [MethodImpl(256)]
        public bool Equals(UnityEngine.Object other) {
            return ReferenceEquals(other, null) ? Id == 0 : Id == other.GetInstanceID();
        }

        [MethodImpl(256)]
        public int CompareTo(RuntimeObjectHandle other) {
            return Id.CompareTo(other.Id);
        }

        #endregion // Interfaces

        #region Lookup

        private delegate UnityEngine.Object FindDelegate(int instanceId);
        private delegate bool AlivePredicate(int instanceId);
        static private readonly FindDelegate s_FindDelegate;
        static private readonly AlivePredicate s_AliveDelegate;

        static RuntimeObjectHandle() {
            MethodInfo findInfo = typeof(UnityEngine.Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static);
            if (findInfo != null) {
                s_FindDelegate = (FindDelegate) findInfo.CreateDelegate(typeof(FindDelegate));
            }

            MethodInfo aliveInfo = typeof(UnityEngine.Object).GetMethod("DoesObjectWithInstanceIDExist", BindingFlags.NonPublic | BindingFlags.Static);
            if (aliveInfo != null) {
                s_AliveDelegate = (AlivePredicate) aliveInfo.CreateDelegate(typeof(AlivePredicate));
            }
        }

        /// <summary>
        /// Finds the Object instance with the given id.
        /// </summary>
        [MethodImpl(256)]
        static public UnityEngine.Object Find(int instanceId) {
            if (instanceId == 0 || s_FindDelegate == null) {
                return null;
            }
            return s_FindDelegate(instanceId);
        }

        /// <summary>
        /// Finds the Object instance with the given id.
        /// Will throw an exception if the Object type is not castable.
        /// </summary>
        [MethodImpl(256)]
        static public T Find<T>(int instanceId) where T : UnityEngine.Object {
            if (instanceId == 0 || s_FindDelegate == null) {
                return null;
            }
            return (T) s_FindDelegate(instanceId);
        }

        /// <summary>
        /// Finds the Object instance with the given id.
        /// </summary>
        [MethodImpl(256)]
        static public T SafeFind<T>(int instanceId) where T : UnityEngine.Object {
            if (instanceId == 0 || s_FindDelegate == null) {
                return null;
            }
            return s_FindDelegate(instanceId) as T;
        }

        /// <summary>
        /// Returns if the Object instance with the given id exists.
        /// </summary>
        [MethodImpl(256)]
        static public bool IsAlive(int instanceId) {
            if (instanceId == 0 || s_AliveDelegate == null) {
                return false;
            }
            return s_AliveDelegate(instanceId);
        }

        #endregion // Lookup

        #region Overrides

        [MethodImpl(256)]
        public override int GetHashCode() {
            return Id;
        }

        [MethodImpl(256)]
        public override bool Equals(object obj) {
            if (obj is RuntimeObjectHandle)
                return Equals((RuntimeObjectHandle) obj);
            if (obj is UnityEngine.Object)
                return Equals((UnityEngine.Object) obj);
            return false;
        }

        [MethodImpl(256)]
        public override string ToString() {
            if (Id == 0) {
                return "Null";
            }
            bool isAlive = IsAlive(Id);
            #if OBJECT_HANDLE_PRESERVE_TYPE
            if (m_TypeHandle.Value != null) {
                return string.Format("Id: {0}, Type: {1}, Alive: {2}", Id, Type.GetTypeFromHandle(m_TypeHandle).FullName, isAlive);
            }
            #endif // OBJECT_HANDLE_PRESERVE_TYPE
            return string.Format("Id: {0}, Alive: {1}", Id, isAlive);
        }

        [MethodImpl(256)]
        static public bool operator==(RuntimeObjectHandle x, RuntimeObjectHandle y) {
            return x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator!=(RuntimeObjectHandle x, RuntimeObjectHandle y) {
            return !x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator==(RuntimeObjectHandle x, UnityEngine.Object y) {
            return x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator!=(RuntimeObjectHandle x, UnityEngine.Object y) {
            return !x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator==(UnityEngine.Object x, RuntimeObjectHandle y) {
            return y.Equals(x);
        }

        [MethodImpl(256)]
        static public bool operator!=(UnityEngine.Object x, RuntimeObjectHandle y) {
            return !y.Equals(x);
        }

        [MethodImpl(256)]
        static public explicit operator int(RuntimeObjectHandle x) {
            return x.Id;
        }

        [MethodImpl(256)]
        static public implicit operator RuntimeObjectHandle(UnityEngine.Object x) {
            return new RuntimeObjectHandle(x);
        }

        #endregion // Overrides
    }

    /// <summary>
    /// Weak typed runtime UnityEngine.Object handle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeObjectHandle<T> : IEquatable<RuntimeObjectHandle<T>>, IEquatable<T>, IComparable<RuntimeObjectHandle<T>>
        where T : UnityEngine.Object {
        
        /// <summary>
        /// Null handle.
        /// </summary>
        static public readonly RuntimeObjectHandle<T> Null = new RuntimeObjectHandle<T>(0);

        /// <summary>
        /// Instance Id.
        /// </summary>
        public readonly int Id;

        #if OBJECT_HANDLE_PRESERVE_TYPE

        private readonly RuntimeTypeHandle m_TypeHandle;

        #endif // OBJECT_HANDLE_PRESERVE_TYPE

        public RuntimeObjectHandle(int instanceId) {
            Id = instanceId;
            #if OBJECT_HANDLE_PRESERVE_TYPE
            m_TypeHandle = default;
            #endif // OBJECT_HANDLE_PRESERVE_TYPE
        }

        public RuntimeObjectHandle(T obj) {
            if (ReferenceEquals(obj, null)) {
                Id = 0;
                #if OBJECT_HANDLE_PRESERVE_TYPE
                m_TypeHandle = default;
                #endif // OBJECT_HANDLE_PRESERVE_TYPE
            } else {
                Id = obj.GetInstanceID();
                #if OBJECT_HANDLE_PRESERVE_TYPE
                m_TypeHandle = obj.GetType().TypeHandle;
                #endif // OBJECT_HANDLE_PRESERVE_TYPE
            }
        }

        #region Cast

        /// <summary>
        /// Returns the Object with this handle.
        /// </summary>
        public T Object {
            [MethodImpl(256)] get { return RuntimeObjectHandle.Find<T>(Id); }
        }

        /// <summary>
        /// Returns the Object with this handle.
        /// </summary>
        [MethodImpl(256)]
        public U Cast<U>() where U : T {
            return RuntimeObjectHandle.Find<U>(Id);
        }

        /// <summary>
        /// Returns the Object with this handle.
        /// </summary>
        [MethodImpl(256)]
        public U SafeCast<U>() where U : T {
            return RuntimeObjectHandle.SafeFind<U>(Id);
        }

        /// <summary>
        /// Returns if the Object with this handle is alive.
        /// </summary>
        [MethodImpl(256)]
        public bool IsAlive() {
            return RuntimeObjectHandle.IsAlive(Id);
        }

        /// <summary>
        /// Returns if this Object was previously valid but is now destroyed.
        /// </summary>
        [MethodImpl(256)]
        public bool WasDestroyed() {
            return Id != 0 && !RuntimeObjectHandle.IsAlive(Id);
        }

        #endregion // Cast

        #region Interfaces

        [MethodImpl(256)]
        public bool Equals(RuntimeObjectHandle<T> other) {
            return Id == other.Id;
        }

        [MethodImpl(256)]
        public bool Equals(RuntimeObjectHandle other) {
            return Id == other.Id;
        }

        [MethodImpl(256)]
        public bool Equals(T other) {
            return ReferenceEquals(other, null) ? Id == 0 : Id == other.GetInstanceID();
        }

        [MethodImpl(256)]
        public int CompareTo(RuntimeObjectHandle<T> other) {
            return Id.CompareTo(other.Id);
        }

        #endregion // Interfaces

        #region Overrides

        [MethodImpl(256)]
        public override int GetHashCode() {
            return Id;
        }

        [MethodImpl(256)]
        public override bool Equals(object obj) {
            if (obj is RuntimeObjectHandle<T>)
                return Equals((RuntimeObjectHandle<T>) obj);
            if (obj is RuntimeObjectHandle)
                return Equals((RuntimeObjectHandle) obj);
            if (obj is T)
                return Equals((T) obj);
            return false;
        }

        [MethodImpl(256)]
        public override string ToString() {
            if (Id == 0) {
                return "Null";
            }
            bool isAlive = RuntimeObjectHandle.IsAlive(Id);
            #if OBJECT_HANDLE_PRESERVE_TYPE
            if (m_TypeHandle.Value != null) {
                return string.Format("Id: {0}, Type: {1}, Alive: {2}", Id, Type.GetTypeFromHandle(m_TypeHandle).FullName, isAlive);
            }
            #endif // OBJECT_HANDLE_PRESERVE_TYPE
            return string.Format("Id: {0}, Alive: {1}", Id, isAlive);
        }

        [MethodImpl(256)]
        static public bool operator==(RuntimeObjectHandle<T> x, RuntimeObjectHandle<T> y) {
            return x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator!=(RuntimeObjectHandle<T> x, RuntimeObjectHandle<T> y) {
            return !x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator==(RuntimeObjectHandle<T> x, RuntimeObjectHandle y) {
            return x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator!=(RuntimeObjectHandle<T> x, RuntimeObjectHandle y) {
            return !x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator==(RuntimeObjectHandle x, RuntimeObjectHandle<T> y) {
            return y.Equals(x);
        }

        [MethodImpl(256)]
        static public bool operator!=(RuntimeObjectHandle x, RuntimeObjectHandle<T> y) {
            return !y.Equals(x);
        }

        [MethodImpl(256)]
        static public bool operator==(RuntimeObjectHandle<T> x, T y) {
            return x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator!=(RuntimeObjectHandle<T> x, T y) {
            return !x.Equals(y);
        }

        [MethodImpl(256)]
        static public bool operator==(T x, RuntimeObjectHandle<T> y) {
            return y.Equals(x);
        }

        [MethodImpl(256)]
        static public bool operator!=(T x, RuntimeObjectHandle<T> y) {
            return !y.Equals(x);
        }

        [MethodImpl(256)]
        static public explicit operator int(RuntimeObjectHandle<T> x) {
            return x.Id;
        }

        [MethodImpl(256)]
        static public implicit operator RuntimeObjectHandle<T>(T x) {
            return new RuntimeObjectHandle<T>(x);
        }

        [MethodImpl(256)]
        static public implicit operator RuntimeObjectHandle(RuntimeObjectHandle<T> x) {
            return new RuntimeObjectHandle(x.Object);
        }

        #endregion // Overrides
    }
}