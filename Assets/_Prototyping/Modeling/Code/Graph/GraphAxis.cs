using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauPools;
using System;
using System.Collections.Generic;
using TMPro;

namespace ProtoAqua.Modeling
{
    public class GraphAxis : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TMP_Text m_MaxY = null;
        [SerializeField] private TMP_Text m_MaxX = null;

        #endregion // Inspector

        public void Load(GraphingUtils.AxisRangePair inPair)
        {
            if (m_MaxY)
                m_MaxY.SetText(inPair.Y.Max.ToString());
            
            if (m_MaxX)
                m_MaxX.SetText(inPair.X.Max.ToString());
        }
    }
}