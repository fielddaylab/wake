using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using UnityEngine;

namespace Aqua
{
    public partial class AnalyticsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "AQUALAB";
        [SerializeField] private int m_AppVersion = 1;
        
        #endregion // Inspector

        private SimpleLog m_Logger;

        #region IService

        protected override void Initialize()
        {
            m_Logger = new SimpleLog(m_AppId, m_AppVersion, null);
        }

        protected override void Shutdown()
        {
            m_Logger?.Flush();
            m_Logger = null;
        }

        #endregion // IService
    }
}