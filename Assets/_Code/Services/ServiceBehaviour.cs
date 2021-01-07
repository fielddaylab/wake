using BeauData;
using UnityEngine;

namespace Aqua
{
    public abstract class ServiceBehaviour : MonoBehaviour, IService
    {
        #region IService

        protected virtual void OnDeregisterService() { }

        protected virtual void AfterRegisterService() { }

        protected virtual void OnRegisterService() { }

        public abstract FourCC ServiceId();

        public virtual int Priority() { return 0; }

        #endregion // IService

        #region Events

        protected virtual void OnEnable()
        {
            Services.AttemptRegister(this);
        }

        protected virtual void OnDisable()
        {
            Services.AttemptDeregister(this);
        }

        protected virtual bool IsLoading()
        {
            return false;
        }

        void IService.OnRegisterService()
        {
            Debug.LogFormat("[{0}] Registering service...", GetType().Name);
            OnRegisterService();
            Debug.LogFormat("[{0}] Finished registering service.", GetType().Name);
        }

        void IService.AfterRegisterService()
        {
            AfterRegisterService();
        }

        void IService.OnDeregisterService()
        {
            Debug.LogFormat("[{0}] Deregistering service...", GetType().Name);
            OnDeregisterService();
            Debug.LogFormat("[{0}] Finished deregistering service.", GetType().Name);
        }

        bool IService.IsLoading()
        {
            return IsLoading();
        }

        #endregion // Events
    }
}