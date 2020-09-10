using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using FieldDay;
using UnityEngine;

namespace ProtoAqua
{
    public partial class AnalyticsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private string m_AppId = "Aqualab";
        [SerializeField] private int m_AppVersion = 1;
        
        #endregion // Inspector

        private SimpleLog m_Logger;

        #region IService

        protected override void OnRegisterService()
        {
            m_Logger = new SimpleLog(m_AppId, m_AppVersion);
        }

        protected override void OnDeregisterService()
        {
            m_Logger?.Flush();
            m_Logger = null;
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.Analytics;
        }

        #endregion // IService
    }
}