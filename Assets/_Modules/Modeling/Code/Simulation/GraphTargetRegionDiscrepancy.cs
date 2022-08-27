using BeauPools;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling
{
    public class GraphTargetRegionDiscrepancy : MonoBehaviour
    {
        #region Inspector

        public RectTransform Layout;
        public Image UpperDot;
        public Image LowerDot;
        public Image Line;
        public Image Circle;

        #endregion // Inspector

        public void SetAllAnchorsY(float min, float max) {
            Layout.SetAnchorsY(min, max);
        }
    }
}
