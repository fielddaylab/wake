using System.Collections.Generic;
using BeauData;
using UnityEngine;

namespace ProtoAqua
{
    public sealed class ServiceCache
    {
        private readonly Dictionary<FourCC, IService> m_CachedServices = new Dictionary<FourCC, IService>();

        /// <summary>
        /// Registers a service.
        /// </summary>
        public void Register(IService inService)
        {
            FourCC id = inService.ServiceId();
            IService existing;
            if (m_CachedServices.TryGetValue(id, out existing))
            {
                if (existing == inService)
                    return;

                m_CachedServices.Remove(id);
                existing.OnDeregisterService();

                Debug.LogFormat("[ServiceCache] Deregistered service '{0}' ({1})", id, existing.GetType().Name);
            }

            m_CachedServices.Add(id, inService);
            inService.OnRegisterService();

            Debug.LogFormat("[ServiceCache] Registered service '{0}' ({1})", id, inService.GetType().Name);
        }

        /// <summary>
        /// Deregisters a service.
        /// </summary>
        public bool Deregister(IService inService)
        {
            FourCC id = inService.ServiceId();
            IService existing;
            if (!m_CachedServices.TryGetValue(id, out existing))
                return false;
            if (existing != inService)
                return false;

            m_CachedServices.Remove(id);
            inService.OnDeregisterService();

            Debug.LogFormat("[ServiceCache] Deregistered service '{0}' ({1})", id, inService.GetType().Name);

            return true;
        }

        /// <summary>
        /// Deregisters the service with the given id.
        /// </summary>
        public bool Deregister(FourCC inId)
        {
            IService existing;
            if (!m_CachedServices.TryGetValue(inId, out existing))
                return false;
            
            m_CachedServices.Remove(inId);
            existing.OnDeregisterService();

            Debug.LogFormat("[ServiceCache] Deregistered service '{0}' ({1})", inId, existing.GetType().Name);

            return true;
        }

        /// <summary>
        /// Returns all services.
        /// </summary>
        public IEnumerable<IService> AllServices()
        {
            return m_CachedServices.Values;
        }

        /// <summary>
        /// Retrieves the service with the given id.
        /// </summary>
        public IService this[FourCC inId]
        {
            get { return Get<IService>(inId); }
        }

        /// <summary>
        /// Retrieves the service with the given id, casted to a particular type.
        /// </summary>
        public T Get<T>(FourCC inId) where T : IService
        {
            IService service;
            m_CachedServices.TryGetValue(inId, out service);
            return (T) service;
        }

        /// <summary>
        /// Returns if a service with the given id is registered.
        /// </summary>
        public bool IsRegistered(FourCC inId)
        {
            return m_CachedServices.ContainsKey(inId);
        }
    }
}