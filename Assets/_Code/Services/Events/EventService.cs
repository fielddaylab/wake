using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public class EventService : ServiceBehaviour
    {
        #region Types

        private struct QueuedEvent
        {
            public readonly StringHash32 Id;
            public readonly object Argument;

            public QueuedEvent(StringHash32 inId, object inArgument)
            {
                Id = inId;
                Argument = inArgument;
            }
        }

        private class HandlerBlock
        {
            public CastableEvent<object> Invoker = new CastableEvent<object>();
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
        private readonly Dictionary<StringHash32, HandlerBlock> m_Handlers = new Dictionary<StringHash32, HandlerBlock>(64);
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
            block.Invoker.Register(inAction, inBinding);

            return this;
        }

        /// <summary>
        /// Registers an event handler, optionally bound to a given object.
        /// </summary>
        public EventService Register(StringHash32 inEventId, Action<object> inActionWithContext, UnityEngine.Object inBinding = null)
        {
            HandlerBlock block;
            if (!m_Handlers.TryGetValue(inEventId, out block))
            {
                block = new HandlerBlock();
                m_Handlers.Add(inEventId, block);
            }
            block.Invoker.Register(inActionWithContext, inBinding);

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
            block.Invoker.Register(inActionWithCastedContext, inBinding);

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
                block.Invoker.Deregister(inAction);
            }

            return this;
        }

        /// <summary>
        /// Deregisters an event handler.
        /// </summary>
        public EventService Deregister(StringHash32 inEventId, Action<object> inActionWithContext)
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Invoker.Deregister(inActionWithContext);
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
                block.Invoker.Deregister(inActionWithCastedContext);
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
                block.Invoker.Clear();
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
                block.Invoker.DeregisterAll(inBinding);
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
        public void Dispatch(StringHash32 inEventId, object inContext = null)
        {
            HandlerBlock block;
            if (m_Handlers.TryGetValue(inEventId, out block))
            {
                block.Invoker.Invoke(inContext);
            }
        }

        /// <summary>
        /// Queues the given event to dispatch at the end of the frame.
        /// </summary>
        public void Queue(StringHash32 inEventId, object inContext = null)
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
                cleanedUpFromDestroyed += block.Invoker.DeregisterAllWithDeadContext();
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
}