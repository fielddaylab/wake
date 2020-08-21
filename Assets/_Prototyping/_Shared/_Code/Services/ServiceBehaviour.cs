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
            OnRegisterService();
        }

        void IService.OnDeregisterService()
        {
            OnDeregisterService();
        }

        #endregion // Events
    }
}