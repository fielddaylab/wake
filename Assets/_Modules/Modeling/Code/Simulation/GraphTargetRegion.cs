using System;
using BeauPools;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class GraphTargetRegion : MonoBehaviour {
        [Serializable] public class Pool : SerializablePool<GraphTargetRegion> { }

        #region Inspector

        public RectTransform Layout;
        public LocText LocText;
        public Graphic Background;
        public GraphTargetRegionDiscrepancy Discrepancy;
        
        #endregion // Inspector

        [NonSerialized] public float MinValue;
        [NonSerialized] public float MaxValue;
    }
}