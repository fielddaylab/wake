using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ObservationUI : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private ScannerDisplay m_ScannerDisplay = null;

        #endregion // Inspector

        public ScannerDisplay Scanner() { return m_ScannerDisplay; }
    }
}