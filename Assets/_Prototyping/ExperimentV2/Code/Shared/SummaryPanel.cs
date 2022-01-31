using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class SummaryPanel : MonoBehaviour
    {
        #region Inspector

        [Header("Header")]
        [Required] public LocText HeaderText;

        [Header("Hint")]
        [Required] public GameObject HintGroup;
        [Required] public LocText HintText;
        
        [Header("Facts")]
        [Required] public GameObject FactGroup;
        [Required] public Transform FactListRoot;
        [InstanceOnly] public FactPools FactPools;
        [Required] public LayoutGroup FactListLayout;

        [Header("Button")]
        [Required] public Button Button;
        [Required] public LocText ButtonLabel;

        #endregion // Inspector
    }
}