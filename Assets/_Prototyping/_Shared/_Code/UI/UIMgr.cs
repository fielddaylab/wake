using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public class UIMgr : MonoBehaviour, IService
    {
        #region Inspector

        #endregion // Inspector

        #region IService

        void IService.OnDeregisterService()
        {
            Debug.LogFormat("[UIMgr] Unloading...");

            Debug.LogFormat("[UIMgr] ...done");
        }

        void IService.OnRegisterService()
        {
            Debug.LogFormat("[UIMgr] Initializing...");

            Debug.LogFormat("[UIMgr] ...done");
        }

        FourCC IService.ServiceId()
        {
            return ServiceIds.CommonUI;
        }

        #endregion // IService

        #region Unity Events

        private void OnEnable()
        {
            Services.UI = this;
        }

        private void OnDisable()
        {
            if (Services.UI == this)
                Services.UI = null;
        }

        #endregion // Unity Events
    }
}