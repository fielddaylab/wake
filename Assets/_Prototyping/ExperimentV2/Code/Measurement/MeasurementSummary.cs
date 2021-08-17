using System;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class MeasurementSummary : MonoBehaviour
    {
        #region Inspector

        [Required] public SummaryPanel Base;
        public LocText.Pool HeaderPool;
        public LocText.Pool TextPool;

        #endregion // Inspector
    }
}