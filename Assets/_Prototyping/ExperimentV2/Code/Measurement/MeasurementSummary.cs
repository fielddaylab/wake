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

        public Color SuccessHeaderColor;
        public Color SuccessTextColor;
        public Color FailureHeaderColor;
        public Color FailureTextColor;

        #endregion // Inspector
    }
}