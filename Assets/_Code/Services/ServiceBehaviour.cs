using BeauData;
using UnityEngine;
using BeauUtil.Services;

namespace Aqua
{
    public abstract class ServiceBehaviour : MonoBehaviour, IService
    {
        #region IService

        protected virtual void Shutdown() { }

        protected virtual void Initialize() { }

        #endregion // IService

        #region Events

        protected virtual void OnDestroy()
        {
            Services.Deregister(this);
        }

        void IService.InitializeService()
        {
            Debug.LogFormat("[{0}] Initializing service...", GetType().Name);
            Initialize();
            Debug.LogFormat("[{0}] Finished initializing service.", GetType().Name);
        }

        void IService.ShutdownService()
        {
            Debug.LogFormat("[{0}] Shutting down service...", GetType().Name);
            Shutdown();
            Debug.LogFormat("[{0}] Finished deregistering service.", GetType().Name);
        }

        #endregion // Events
    }
}