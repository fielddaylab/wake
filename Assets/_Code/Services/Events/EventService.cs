#if UNITY_2021_2_OR_NEWER && !BEAUUTIL_DISABLE_FUNCTION_POINTERS
#define SUPPORTS_FUNCTION_POINTERS
#endif // UNITY_2021_2_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using UnityEngine;

using HandlerBlock = BeauUtil.CastableEvent<Aqua.EvtArgs>;

namespace Aqua
{
    public class EventService : ServiceBehaviour
    {
        #region Types

        private struct QueuedEvent
        {
            public readonly StringHash32 Id;
            public readonly EvtArgs Argument;

            public QueuedEvent(StringHash32 inId, EvtArgs inArgument)
            {
                Id = inId;
                Argument = inArgument;
            }
        }

        // Implements the event listener
        private sealed class WaitForEventHandler : IEnumerator, IDisposable
        {
            private StringHash32 m_EventId;
            private Action m_Listener;
            private int m_Phase = 0; // 0 uninitialized 1 waiting 2 done

            public WaitForEventHandler(StringHash32 inEventId)
            {
                m_EventId = inEventId;
                m_Phase = 0;
                m_Listener = OnInvoke;
            }

            public object Current { get { return null; } }

            public void Dispose()
            {
                if (m_Phase > 0)
                {
                    Services.Events?.Deregister(m_EventId, m_Listener);
                }

                m_Phase = 0;
                m_EventId = null;
                m_Listener = null;
            }

            public bool MoveNext()
            {
                switch (m_Phase)
                {
                    case 0:
                        m_Phase = 1;
                        Services.Events.Register(m_EventId, m_Listener);
                        return true;

                    case 2:
                        return false;

                    case 1:
                    default:
                        return true;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            private void OnInvoke()
            {
                m_Phase = 2;
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private float m_CleanupInterval = 30;

        #endregion // Inspector

        private Routine m_CleanupRoutine;
        private readonly Dictionary<StringHash32, HandlerBlock> m_Handlers = Collections.NewDictionary<StringHash32, HandlerBlock>(64);
        private readonly RingBuffer<QueuedEvent> m_QueuedEvents = new RingBuffer<QueuedEvent>(64, RingBufferMode.Expand);

        #region Registration

        /// <summary>
        /// Registers an event handler, optionally bound to a given object.
        /// </summary>
        public EventService Register(StringHash32 inEventId, Action inAction, UnityEngine.Object inBinding = null)
        {
            HandlerBlock block;
            if (!m_Handlers.TryGetValue(inEventId, out block))
            {
                block = new HandlerBlock();
                m_Handlers.Add(inEventId, block);
            }
            block.Register(inAction, inBinding);

            return this;
        }

        /// <summary>
        /// Registers an event handler, optionally bound to a given object.
        /// </summary>
        public EventService Register(StringHash32 inEventId, Action<EvtArgs> inActionWithContext, UnityEngine.Object inBinding = null)
        {
            HandlerBlock block;
            if (!m_Handlers.TryGetValue(inEventId, out block))
            {
                block = new HandlerBlock();
                m_Handlers.Add(inEventId, block);
            }
            block.Register(inActionWithContext, inBinding);

            return this;
        }

        /// <summary>
        /// Registers an event handler, optionally bound to a given object.
        /// </summary>
        public EventService Register<T>(StringHash32 inEventId, Action<T> inActionWithCastedContext, UnityEngine.Object inBinding = null)
        {
            HandlerBlock block;
            if (!m_Handlers.TryGetValue(inEventId, out block))
            {
                block = new HandlerBlock();
                m_Handlers.Add(inEventId, block);
            }
            block.Register(inActionWithCastedContext, inBinding);

            return this;
        }

        /// <summary>
        /// Deregisters an event handler.
        /// </summary>
        public EventService Deregister(StringHash32 inEventId, Action inAction)
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Deregister(inAction);
            }

            return this;
        }

        /// <summary>
        /// Deregisters an event handler.
        /// </summary>
        public EventService Deregister(StringHash32 inEventId, Action<EvtArgs> inActionWithContext)
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Deregister(inActionWithContext);
            }

            return this;
        }

        /// <summary>
        /// Deregisters an event handler.
        /// </summary>
        public EventService Deregister<T>(StringHash32 inEventId, Action<T> inActionWithCastedContext)
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Deregister(inActionWithCastedContext);
            }

            return this;
        }

        /// <summary>
        /// Deregisters all handlers for the given event.
        /// </summary>
        public EventService DeregisterAll(StringHash32 inEventId)
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Clear();
            }

            return this;
        }

        /// <summary>
        /// Deregisters all handlers associated with the given object.
        /// </summary>
        public EventService DeregisterAll(UnityEngine.Object inBinding)
        {
            if (inBinding.IsReferenceNull())
                return this;

            foreach(var block in m_Handlers.Values)
            {
                block.DeregisterAll(inBinding);
            }

            return this;
        }

        #endregion // Registration

        #region Async

        /// <summary>
        /// Waits for the given event to execute.
        /// </summary>
        public IEnumerator WaitForEvent(StringHash32 inEventId)
        {
            return new WaitForEventHandler(inEventId);
        }

        #endregion // Async

        #region Operations

        /// <summary>
        /// Dispatches the given event with an optional argument.
        /// </summary>
        public void Dispatch(StringHash32 inEventId, EvtArgs inContext = default(EvtArgs))
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Invoke(inContext);
            }
        }

        /// <summary>
        /// Queues the given event to dispatch at the end of the frame.
        /// </summary>
        public void Queue(StringHash32 inEventId, EvtArgs inContext = default(EvtArgs))
        {
            m_QueuedEvents.PushBack(new QueuedEvent(inEventId, inContext));
        }

        /// <summary>
        /// Cleans up all floating handlers.
        /// </summary>
        public void Cleanup()
        {
            int cleanedUpFromDestroyed = 0;
            foreach(var block in m_Handlers.Values)
            {
                cleanedUpFromDestroyed += block.DeregisterAllWithDeadContext();
            }

            if (cleanedUpFromDestroyed > 0)
            {
                Log.Warn("[EventService] Cleaned up {0} event listeners whose bindings were destroyed", cleanedUpFromDestroyed);
            }
        }

        #endregion // Operations

        private void LateUpdate()
        {
            Flush();
        }

        public void Flush()
        {
            QueuedEvent evt;
            while(m_QueuedEvents.TryPopFront(out evt))
            {
                Dispatch(evt.Id, evt.Argument);
            }
        }

        #region Maintenance

        private IEnumerator MaintenanceRoutine()
        {
            object delay = m_CleanupInterval;
            while(true)
            {
                yield return delay;
                Cleanup();
            }
        }

        private void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            Cleanup();
        }

        #endregion // Maintenance

        #region IService

        protected override void Initialize()
        {
            m_CleanupRoutine.Replace(this, MaintenanceRoutine());

            SceneHelper.OnSceneLoaded += OnSceneLoad;
        }

        protected override void Shutdown()
        {
            m_Handlers.Clear();
            m_CleanupRoutine.Stop();

            SceneHelper.OnSceneLoaded -= OnSceneLoad;
        }

        #endregion // IService
    }

    /// <summary>
    /// Event arguments struct. Represents unmanaged data, boxed structs, and object references.
    /// </summary>
    public struct EvtArgs {
        // would be great to have a queued event data fit entirely on a cache line
        // 64 bytes total, minus the event id and object reference (assume 64bit pointer)
        private const int MaxUnmanagedSize = (int) (64 - 4 - 8);

        private struct UnmanagedData {
            public unsafe fixed ulong Data[MaxUnmanagedSize / 8];
        }

        private object m_Instance;
        private UnmanagedData m_Unmanaged;

        #region Accessors

        public T Unbox<T>() where T : struct {
            return (T) m_Instance;
        }

        public T Deref<T>() where T : class {
            return (T) m_Instance;
        }

        public T Unpack<T>() where T : unmanaged {
            return Unsafe.FastReinterpret<UnmanagedData, T>(m_Unmanaged);
        }

        #endregion // Accessors

        #region Operators

        static public implicit operator EvtArgs(sbyte data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(byte data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(short data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(ushort data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(char data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(int data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(uint data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(long data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(ulong data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(float data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(double data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(bool data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(string data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(StringHash32 data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(StringHash64 data) {
            return Create(data);
        }

        static public implicit operator EvtArgs(RuntimeObjectHandle data) {
            return Create(data);
        }

        #endregion // Operators

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public EvtArgs Create<T>(T data) where T : unmanaged {
            return UnmanagedConverter<T>.Create(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public EvtArgs Create(string data) {
            return BoxedConverter<string>.Create(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public EvtArgs Ref<T>(T data) where T : class {
            return BoxedConverter<T>.Create(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public EvtArgs Box<T>(T data) where T : struct {
            return BoxedConverter<T>.Create(data);
        }

        #endregion // Constructors

        #region Converters

        static private unsafe class UnmanagedConverter<T> where T : unmanaged {
            static UnmanagedConverter() {
                Assert.True(sizeof(T) <= sizeof(UnmanagedData), "Unmanaged type '{0}' exceeds the maximum allowed size for an EvtData ({1} > {2})", typeof(T).FullName, sizeof(T), sizeof(UnmanagedData));
                Log.Msg("[EvtArgs] Registering unmanaged converter from '{0}' to '{1}'", typeof(EvtArgs).FullName, typeof(T).FullName);
#if SUPPORTS_FUNCTION_POINTERS
                CastableArgument.RegisterConverter<EvtArgs, T>(&Cast);
#else
                CastableArgument.RegisterConverter<EvtArgs, T>(Cast);
#endif // SUPPORTS_FUNCTION_POINTERS
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static private T Cast(EvtArgs args) {
                Assert.True(ReferenceEquals(args.m_Instance, typeof(T)), "Mismatched create/cast between '{0}' and '{1}'", ((Type) args.m_Instance).FullName, typeof(T).FullName);
                return Unsafe.FastReinterpret<ulong, T>(args.m_Unmanaged.Data);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public EvtArgs Create(T data) {
                EvtArgs dat = default;
                *(T*) (&dat.m_Unmanaged) = data;
                dat.m_Instance = typeof(T);
                return dat;
            }
        }

        static private unsafe class BoxedConverter<T> {
            static BoxedConverter() {
                //Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<T>(), "Unmanaged type '{0}' passed into 'EvtArgs.Box'", typeof(T).FullName);
                Log.Msg("[EvtArgs] Registering managed converter from '{0}' to '{1}'", typeof(EvtArgs).FullName, typeof(T).FullName);
#if SUPPORTS_FUNCTION_POINTERS
                CastableArgument.RegisterConverter<EvtArgs, T>(&Cast);
#else
                CastableArgument.RegisterConverter<EvtArgs, T>(Cast);
#endif // SUPPORTS_FUNCTION_POINTERS
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static private T Cast(EvtArgs args) {
                return (T) args.m_Instance;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public EvtArgs Create(T data) {
                EvtArgs dat = default;
                dat.m_Instance = data;
                return dat;
            }
        }

        #endregion // Converters
    }
}