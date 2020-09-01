using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace ProtoAqua.Observation
{
    public class ObservationUI : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private ScannerDisplay m_ScannerDisplay = null;

        #endregion // Inspector

        public ScannerDisplay Scanner() { return m_ScannerDisplay; }

        #region Service

        static public readonly FourCC Id = ServiceIds.Register("OBUI", "Observation UI");

        public override FourCC ServiceId()
        {
            return Id;
        }

        #endregion // Service
    }
}