using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class SimGraphBlock : MonoBehaviour, IPoolAllocHandler {
        [Serializable] public class Pool : SerializablePool<SimGraphBlock> { }

        public enum RenderMask {
            Historical = 0x01,
            Player = 0x02,
            Predict = 0x04
        }

        #region Inspector

        [Header("Base")]
        public RectTransform IconPin;
        public Image Icon;
        public Graphic IconBG;

        [Header("Describe")]
        public GraphLineRenderer Historical;
        public GraphLineRenderer Player;
        public GraphLineFillRenderer Fill;

        [Header("Predict")]
        public GraphLineRenderer Predict;

        [Header("Intervene")]
        public Graphic InterventionTarget;

        #endregion // Inspector

        [NonSerialized] public StringHash32 ActorId;
        [NonSerialized] public WaterPropertyId PropertyId = WaterPropertyId.NONE;
        [NonSerialized] public Color PrimaryColor;
        [NonSerialized] public RenderMask AppliedScaleMask;
        [NonSerialized] public Rect LastRect;
        [NonSerialized] public Rect LastRectHistorical;
        [NonSerialized] public Rect LastRectPlayer;
        [NonSerialized] public Rect LastRectPredict;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            LastRect = default;
            AppliedScaleMask = 0;
            PropertyId = WaterPropertyId.NONE;
            ActorId = default;
        }
    }
}