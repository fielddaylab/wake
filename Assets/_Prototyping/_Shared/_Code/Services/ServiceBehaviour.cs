using BeauData;
using UnityEngine;

namespace ProtoAqua
{
    public abstract class ServiceBehaviour : MonoBehaviour, IService
    {
        #region IService

        protected virtual void OnDeregisterService() { }

        protected virtual void OnRegisterService() { }

        public abstract FourCC ServiceId();

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

        void IService.OnRegisterService()
        {
            Debug.LogFormat("[{0}] Registering service...", GetType().Name);
            OnRegisterService();
            Debug.LogFormat("[{0}] Finished registering service.", GetType().Name);
        }

        void IService.OnDeregisterService()
        {
            Debug.LogFormat("[{0}] Deregistering service...", GetType().Name);
            OnDeregisterService();
            Debug.LogFormat("[{0}] Finished deregistering service.", GetType().Name);
        }

        #endregion // Events
    }
}