using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public class UIMgr : ServiceBehaviour
    {
        #region Inspector

        #endregion // Inspector

        #region IService

        protected override void OnDeregisterService()
        {
            Debug.LogFormat("[UIMgr] Unloading...");

            Debug.LogFormat("[UIMgr] ...done");
        }

        protected override void OnRegisterService()
        {
            Debug.LogFormat("[UIMgr] Initializing...");

            Debug.LogFormat("[UIMgr] ...done");
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.CommonUI;
        }

        #endregion // IService
    }
}