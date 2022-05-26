using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class SimGraphBlock : MonoBehaviour, IPoolAllocHandler {
        [Serializable] public class Pool : SerializablePool<SimGraphBlock> { }

        #region Inspector

        [Header("Base")]
        public LayoutElement Layout;
        public RectTransform IconPin;
        public Image Icon;
        public Graphic IconBG;

        [Header("Describe")]
        public CanvasGroup DescribeGroup;
        public GraphLineRenderer Historical;
        public GraphLineRenderer Player;
        public GraphLineFillRenderer Fill;

        [Header("Predict")]
        public CanvasGroup PredictGroup;
        public GraphLineRenderer Predict;

        #endregion // Inspector

        [NonSerialized] public StringHash32 ActorId;
        [NonSerialized] public WaterPropertyId PropertyId = WaterPropertyId.NONE;
        [NonSerialized] public Color PrimaryColor;
        [NonSerialized] public Rect LastRect;
        [NonSerialized] public Rect LastRectHistorical;
        [NonSerialized] public Rect LastRectPlayer;
        [NonSerialized] public Rect LastRectPredict;
        [NonSerialized] public TempAlloc<GraphTargetRegion> Intervention;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            PropertyId = WaterPropertyId.NONE;
            ActorId = default;
            LastRectHistorical = default;
            LastRectPlayer = default;
            LastRectPredict = default;
            Ref.Dispose(ref Intervention);
        }
    }

    [Flags]
    public enum SimRenderMask {
        Historical = 0x01,
        Player = 0x02,
        Predict = 0x04,
        Fill = 0x08,
        Intervene = 0x10,

        HistoricalPlayer = Historical | Player,
        HistoricalPlayerFill = HistoricalPlayer | Fill,
        PredictIntervene = Predict | Intervene,

        AllData = Historical | Player | Predict
    }
}